using System.Diagnostics;

class Program
{
    static void Main()
    {
        try
        {
            Console.WriteLine("Enter the size of the file in GBs:");
            double fileSizeInGB = Convert.ToDouble(Console.ReadLine());

            Console.WriteLine("Enter the number of repeated lines:");
            int numRepeatedLines = Convert.ToInt32(Console.ReadLine());

            long fileSizeInBytes = ConvertGBToBytes(fileSizeInGB);

            Console.WriteLine("Generating file...");

            Stopwatch stopwatch = Stopwatch.StartNew();
            GenerateFileParallel(fileSizeInBytes, numRepeatedLines, "output.txt");
            stopwatch.Stop();
            Console.WriteLine($"Time spent during file generation: {stopwatch.Elapsed.TotalSeconds} seconds");
            Console.WriteLine("File generated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }

        Console.ReadLine();
    }

    private static long ConvertGBToBytes(double gigabytes)
    {
        if (gigabytes < 0)
        {
            throw new ArgumentException("File size cannot be negative.");
        }

        double bytes = gigabytes * 1024 * 1024 * 1024;

        if (bytes > long.MaxValue)
        {
            throw new OverflowException("File size exceeds the maximum value.");
        }

        return (long)bytes;
    }

    private static void GenerateFileParallel(long fileSizeInBytes, int numRepeatedLines, string filePath)
    {
        Random random = new Random();
        long bytesWritten = 0;
        int chunkSize = CalculateChunkSize(fileSizeInBytes);
        Console.WriteLine("chunkSize:" + BytesToMegabytes(chunkSize) + " MB");


        using (StreamWriter writer = new StreamWriter(filePath))
        {
            object lockObj = new object();

            // Divide the file size equally among the threads
            int numThreads = Environment.ProcessorCount;
            long bytesPerThread = fileSizeInBytes / numThreads;

            Parallel.For(0, numThreads, threadIndex =>
            {
                long startByte = threadIndex * bytesPerThread;
                long endByte = startByte + bytesPerThread;

                if (threadIndex == numThreads - 1)
                {
                    // Adjust the end byte for the last thread to account for any remaining bytes
                    endByte = fileSizeInBytes;
                }

                lock (lockObj)
                {
                    // Seek to the starting position for each thread to write its lines
                    writer.BaseStream.Seek(startByte, SeekOrigin.Begin);

                    while (bytesWritten < endByte)
                    {
                        string[] lines = GenerateLines(random, numRepeatedLines);

                        foreach (string line in lines)
                        {
                            writer.WriteLine(line);
                            bytesWritten += line.Length + 2; // Account for line terminator (CR+LF)

                            if (bytesWritten % chunkSize == 0)
                            {
                                writer.Flush();
                            }

                            if (bytesWritten >= endByte)
                            {
                                break;
                            }
                        }
                    }
                }
            });
        }
    }

    private static int CalculateChunkSize(long fileSize)
    {
        const int minChunkSize = 64 * 1024; // 64 KB
        const int maxChunkSize = 10 * 1024 * 1024; // 10 MB

        long availableMemory = GC.GetTotalMemory(false);
        long availableMemoryForFile = availableMemory / 2; // Use half of available memory for file writing

        long desiredChunkSize = fileSize / 100; // Aim for 1% of the desired file size as the chunk size

        // Adjust the chunk size based on available memory
        int chunkSize = (int)Math.Min(Math.Max(desiredChunkSize, minChunkSize), maxChunkSize);
        while (chunkSize > availableMemoryForFile)
        {
            chunkSize /= 2; // Reduce the chunk size until it fits within the available memory
        }

        return chunkSize;
    }


    private static string[] GenerateLines(Random random, int numRepeatedLines)
    {
        List<string> lines = new List<string>();

        string[] words = { "Apple", "Banana", "Cherry", "Flower", "Something" };

        int number = random.Next();
        string word = words[random.Next(words.Length)];

        if (numRepeatedLines > 0)
        {
            for (int i = 0; i < numRepeatedLines; i++)
            {
                string line = $"{number}. {word}";
                lines.Add(line);
            }
        }
        else
        {
            string line = $"{number}. {word}";
            lines.Add(line);
        }

        return lines.ToArray();
    }
    private static double BytesToMegabytes(long bytes)
    {
        const int bytesInMegabyte = 1024 * 1024;
        return (double)bytes / bytesInMegabyte;
    }

}