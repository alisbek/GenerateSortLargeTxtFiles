using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FileSortingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the path of the input file:");
            string inputFilePath = Console.ReadLine();

            Console.WriteLine("Enter the name of the output file:");
            string outputFileName = Console.ReadLine();
            string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath), outputFileName);
            Stopwatch stopwatch = Stopwatch.StartNew();
            Console.WriteLine("Sorting file in progress...");
            FileSorter.SortFile(inputFilePath, outputFilePath);
            stopwatch.Stop();
            Console.WriteLine($"Time spent during file sorting: {stopwatch.Elapsed.TotalSeconds} seconds");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
