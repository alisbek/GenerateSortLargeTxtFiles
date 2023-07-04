using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace FileSortingApp
{
    public class Programe
    {
        public static void Maind()
        {
            Console.WriteLine("Enter the path to the input file:");
            string inputFilePath = "10.txt";

            Console.WriteLine("Sorting in progress...");

            try
            {
                string outputFilePath = "result10.txt";
                SortFile(inputFilePath, outputFilePath);

                Console.WriteLine("Sorting completed successfully.");
                Console.WriteLine("Output file: " + outputFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during sorting: " + ex.Message);
            }

            Console.ReadLine();
        }

        static void SortFile(string inputFilePath, string outputFilePath)
        {
            long fileSize = new FileInfo(inputFilePath).Length;

            if (fileSize > 2.5 * 1024 * 1024 * 1024) // Greater than 2.5 GB
            {
                long chunkSize = CalculateChunkSize(fileSize);
                SortFileWithChunking(inputFilePath, outputFilePath, chunkSize);
            }
            else
            {
                SortFileInMemory(inputFilePath, outputFilePath);
            }
        }

        static void SortFileWithChunking(string inputFilePath, string outputFilePath, long chunkSize)
        {
            var tempFilePaths = new List<string>();
            var chunkSizes = CalculateChunkSizes(new FileInfo(inputFilePath).Length, chunkSize);

            try
            {
                SplitFileIntoChunks(inputFilePath, tempFilePaths, chunkSizes);
                Parallel.ForEach(tempFilePaths, (tempFilePath) =>
                {
                    SortFileInMemory(tempFilePath, tempFilePath);
                });
                MergeSortedFiles(tempFilePaths, outputFilePath);
            }
            finally
            {
                foreach (var tempFilePath in tempFilePaths)
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        static void SplitFileIntoChunks(string inputFilePath, List<string> tempFilePaths, List<int> chunkSizes)
        {
            using (var inputFileStream = File.OpenRead(inputFilePath))
            {
                foreach (var chunkSize in chunkSizes)
                {
                    var tempFilePath = Path.GetTempFileName();
                    tempFilePaths.Add(tempFilePath);

                    using (var tempFileStream = File.OpenWrite(tempFilePath))
                    {
                        var buffer = new byte[chunkSize];
                        int bytesRead;
                        int totalBytesRead = 0;
                       
                        while ((bytesRead = inputFileStream.Read(buffer, 0, buffer.Length)) > 0 && totalBytesRead < chunkSize)
                        {
                            tempFileStream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                        }
                    }
                }
            }
        }

        static void SortFileInMemory(string inputFilePath, string outputFilePath)
        {
            var lines = new List<string>();
            long readLine = 0;
            using (var inputFileStream = File.OpenRead(inputFilePath))
            using (var streamReader = new StreamReader(inputFileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (++readLine % 5000 == 0)
                        Console.Write("{0:f2}%   \r", 100.0 * streamReader.BaseStream.Position / streamReader.BaseStream.Length);
                    var parts = line.Split('.');
                    if (parts.Length >= 2)
                    {
                        var numberPart = parts[0].Trim();
                        var textPart = parts[1].Trim();

                        if (!string.IsNullOrEmpty(numberPart) && !string.IsNullOrEmpty(textPart))
                        {
                            lines.Add(line);
                        }
                    }
                }
            }

            var sortedLines = lines
                .Select(line =>
                {
                    var parts = line.Split('.');
                    return new { Number = long.Parse(parts[0].Trim()), Text = parts[1].Trim() };
                })
                .OrderBy(x => x.Text)
                .ThenBy(x => x.Number)
                .Select(x => $"{x.Number}. {x.Text}")
                .ToList();

            File.WriteAllLines(outputFilePath, sortedLines);
        }


        static void MergeSortedFiles(List<string> sortedFiles, string outputFilePath)
        {
            var fileStreams = sortedFiles.Select(file => File.OpenText(file)).ToList();
            using (var outputStream = File.CreateText(outputFilePath))
            {
                var heap = new SortedDictionary<string, List<string>>();
                foreach (var stream in fileStreams)
                {
                    var line = stream.ReadLine();
                    if (line != null)
                    {
                        var key = line.Split('.')[1].Trim();
                        if (!heap.ContainsKey(key))
                            heap[key] = new List<string>();
                        heap[key].Add(line);
                    }
                }

                while (heap.Count > 0)
                {
                    var minKey = heap.Keys.First();
                    var minValues = heap[minKey];
                    if (minValues.Count > 1)
                    {
                        minValues.Sort((x, y) =>
                        {
                            var xNumber = long.Parse(x.Split('.')[0]);
                            var yNumber = long.Parse(y.Split('.')[0]);
                            return xNumber.CompareTo(yNumber);
                        });
                    }

                    foreach (var value in minValues)
                    {
                        outputStream.WriteLine(value);
                    }

                    minValues.Clear();
                    heap.Remove(minKey);

                    foreach (var stream in fileStreams.ToList())
                    {
                        var line = stream.ReadLine();
                        if (line != null)
                        {
                            var key = line.Split('.')[1].Trim();
                            if (!heap.ContainsKey(key))
                                heap[key] = new List<string>();
                            heap[key].Add(line);
                        }
                        else
                        {
                            stream.Dispose();
                            fileStreams.Remove(stream);
                        }
                    }
                }
            }
        }

        static long CalculateChunkSize(long fileSize)
        {
            const int defaultChunkSize = 50 * 1024 * 1024; // 50 MB

            if (fileSize <= defaultChunkSize)
            {
                return (int)fileSize;
            }
            else
            {
                long maxChunkSize = unchecked(2 * 1024 * 1024 * 1024); // 2 GB
                int chunkSize = defaultChunkSize;

                while (chunkSize < maxChunkSize)
                {
                    chunkSize *= 2;

                    if (fileSize <= chunkSize)
                        return chunkSize;
                }

                return maxChunkSize;
            }
        }

        static List<int> CalculateChunkSizes(long fileSize, long targetChunkSize)
        {
            const int minChunkSize = 1024 * 1024; // 1 MB
            var chunkSizes = new List<int>();
            long remainingBytes = fileSize;
            int chunkCount = (int)Math.Ceiling((double)fileSize / targetChunkSize);

            if (chunkCount == 1)
            {
                chunkSizes.Add((int)fileSize);
            }
            else
            {
                int averageChunkSize = (int)Math.Ceiling((double)fileSize / chunkCount);
                int chunkSize = Math.Max(minChunkSize, averageChunkSize);
                remainingBytes -= chunkSize;
                chunkSizes.Add(chunkSize);

                while (chunkSizes.Count < chunkCount - 1)
                {
                    chunkSize = Math.Min(chunkSize, (int)remainingBytes);
                    remainingBytes -= chunkSize;
                    chunkSizes.Add(chunkSize);
                }

                if (remainingBytes > 0)
                    chunkSizes.Add((int)remainingBytes);
            }

            return chunkSizes;
        }
    }
}
