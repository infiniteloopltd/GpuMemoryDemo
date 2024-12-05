using Cloo;
using System;
using System.Collections.Generic;
using System.Text;

namespace FiachCloo
{
    internal class Averages
    {
        static void Test(string[] args)
        {
            // 1. Generate large lists of random numbers
            int dataSize = 100000000; // Size of the array
            float[] A = new float[dataSize];
            float[] B = new float[dataSize];
            float[] C = new float[dataSize]; // Result array

            Random rand = new Random();
            for (int i = 0; i < dataSize; i++)
            {
                A[i] = (float)(rand.NextDouble() * 100); // Random number between 0 and 100
                B[i] = (float)(rand.NextDouble() * 100); // Random number between 0 and 100
            }

            // 2. Initialize OpenCL resources
            var platform = ComputePlatform.Platforms[0]; // Use the first available platform
            var device = platform.Devices[0]; // Use the first available device (GPU)
            var context = new ComputeContext(ComputeDeviceTypes.Gpu, new ComputeContextPropertyList(platform), null, IntPtr.Zero);
            var queue = new ComputeCommandQueue(context, device, ComputeCommandQueueFlags.None);

            // 3. Create OpenCL buffers
            var bufferA = new ComputeBuffer<float>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, A);
            var bufferB = new ComputeBuffer<float>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, B);
            var bufferC = new ComputeBuffer<float>(context, ComputeMemoryFlags.WriteOnly, dataSize);

            // 4. Write OpenCL kernel to add arrays
            string kernelSource = @"
            __kernel void add_arrays(__global const float* A, __global const float* B, __global float* C, const unsigned int size) {
                int id = get_global_id(0);
                if (id < size) {
                    C[id] = A[id] + B[id];
                }
            }";

            var program = new ComputeProgram(context, kernelSource);
            program.Build(null, null, null, IntPtr.Zero);

            // 5. Create and set up the kernel
            var kernel = program.CreateKernel("add_arrays");
            kernel.SetMemoryArgument(0, bufferA);
            kernel.SetMemoryArgument(1, bufferB);
            kernel.SetMemoryArgument(2, bufferC);
            kernel.SetValueArgument(3, dataSize);

            // 6. Enqueue the kernel to process data

            var startGpu = DateTime.Now;

            queue.Execute(kernel, null, new long[] { dataSize }, null, null);



            queue.ReadFromBuffer(bufferC, ref C, true, null);

            var endGpu = DateTime.Now;

            Console.WriteLine($"GPU time: {endGpu - startGpu}");

            // 8. Output the first 10 results
            Console.WriteLine("First 10 results from GPU:");
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"C[{i}] = {C[i]}");
            }

            // 9. Compare with CPU
            float[] cpuResult = new float[dataSize];
            var startCpu = DateTime.Now;
            for (int i = 0; i < dataSize; i++)
            {
                cpuResult[i] = A[i] + B[i];
            }
            var endCpu = DateTime.Now;

            // Output CPU time
            Console.WriteLine($"CPU time: {endCpu - startCpu}");
        }
    }
}
