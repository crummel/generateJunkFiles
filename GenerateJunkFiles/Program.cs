using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenerateJunkFiles
{
    class Program
    {
        static void Main(string[] args)
        {
            var rand = new Random();
            long targetSize;
            long targetNumber;
            if (args.Count() == 2)
            {
                targetSize = long.Parse(args[0]);
                targetNumber = long.Parse(args[1]);
            }
            else
            {
                targetSize = (long)(rand.NextDouble() * 10L * 1024L * 1024L * 1024L); // 0-10 GB
                targetNumber = rand.Next(1000, 250000);
            }
            double averageSize = (targetSize - 1L * 1024L * 1024L * 1024L) / (double)targetNumber;
            var filesPerFolder = 2500;
            var folderCounter = 0;
            string currentFolder = null;
            Console.WriteLine($"Generating {targetNumber} files with total size {(targetSize / (double)(1024L * 1024L * 1024L)).ToString("F3")} GB");
            while (targetNumber-- > 0)
            {
                if (folderCounter == 0)
                {
                    folderCounter = filesPerFolder;
                    currentFolder = CreateNewFolder();
                    Console.WriteLine($"{targetNumber} files with size {(targetSize / (double)(1024L * 1024L * 1024L)).ToString("F3")} GB remaining");
                }
                var size = GetRandomSize(rand, averageSize, averageSize);
                targetSize -= size;
                CreateSparseFile(currentFolder, size);
                folderCounter--;
            }
            if (targetSize > 0)
            {
                CreateSparseFile(currentFolder, targetSize);
            }
        }

        private static string CreateNewFolder()
        {
            var folderName = Path.GetRandomFileName().Split('.')[0];
            Directory.CreateDirectory(folderName);
            return folderName;
        }

        private static void CreateSparseFile(string folder, long targetSize)
        {
            var filename = Path.Combine(folder, Path.GetRandomFileName());
            using (var fs = File.Create(filename))
            {
                MarkAsSparseFile(fs.SafeFileHandle);
                fs.SetLength(targetSize);
            }
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            int dwIoControlCode,
            IntPtr InBuffer,
            int nInBufferSize,
            IntPtr OutBuffer,
            int nOutBufferSize,
            ref int pBytesReturned,
            [In] ref NativeOverlapped lpOverlapped
        );

        static void MarkAsSparseFile(SafeFileHandle fileHandle)
        {
            int bytesReturned = 0;
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            bool result =
                DeviceIoControl(
                    fileHandle,
                    590020, //FSCTL_SET_SPARSE,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0,
                    ref bytesReturned,
                    ref lpOverlapped);
            if (result == false)
                throw new Win32Exception();
        }

        static long GetRandomSize(Random r, double mean, double stdDev)
        {
            long size = -1;
            var u1 = r.NextDouble();
            var u2 = r.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            var randNormal = mean + stdDev * randStdNormal;
            size = (long)(randNormal + 1);
            return Math.Abs(size);
        }
    }
}
