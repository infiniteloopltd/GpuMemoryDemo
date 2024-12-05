using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Cloo;

namespace FiachCloo
{
    internal class Program
    {

        static void Main()
        {
            var platform = ComputePlatform.Platforms[0];
            var device = platform.Devices.FirstOrDefault(d => d.Type.HasFlag(ComputeDeviceTypes.Gpu));
            var context = new ComputeContext(ComputeDeviceTypes.Gpu, new ComputeContextPropertyList(platform), null, IntPtr.Zero);
            var queue = new ComputeCommandQueue(context, device, ComputeCommandQueueFlags.None);

            const string largeFilePath = "C:\\Users\\fiach\\Downloads\\datagrip-2024.3.exe";
            var contents = File.ReadAllBytes(largeFilePath);

            var clBuffer = Store(contents, context, queue);

            var readBackBytes = Retrieve(contents.Length, clBuffer, queue);

            Console.WriteLine($"Original String: {contents[0]}");
            Console.WriteLine($"Read Back String: {readBackBytes[0]}");
            Console.WriteLine($"Strings Match: {contents[0] == readBackBytes[0]}");

            
            // Memory leak here. 
            //Marshal.FreeHGlobal(readBackPtr);
            //Marshal.FreeHGlobal(buffer);
            
        }

        public static ComputeBuffer<byte> Store(byte[] stringBytes, ComputeContext context, ComputeCommandQueue queue)
        {
            var buffer = Marshal.AllocHGlobal(stringBytes.Length);

            Marshal.Copy(stringBytes, 0, buffer, stringBytes.Length);

            var clBuffer = new ComputeBuffer<byte>(context, ComputeMemoryFlags.ReadWrite, stringBytes.Length);

            queue.Write(clBuffer, true, 0, stringBytes.Length, buffer, null);
            
            return clBuffer;
        }

        public static byte[] Retrieve(int size, ComputeBuffer<byte> clBuffer, ComputeCommandQueue queue)
        {
            var readBackPtr = Marshal.AllocHGlobal(size);

            queue.Read(clBuffer, true, 0, size, readBackPtr, null);

            var readBackBytes = new byte[size];

            Marshal.Copy(readBackPtr, readBackBytes, 0, size);

            return readBackBytes;
        }
    }
}
