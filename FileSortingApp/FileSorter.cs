namespace FileSortingApp;

public class FileSorter
{
    private const int ChunkSize = 1000000; // Chunk size of 1 million lines

    public static void SortFile(string inputFilePath, string outputFilePath)
    {
        try
        {
            var fileSize = new FileInfo(inputFilePath).Length;

            if (fileSize > 2500000000) // If file size is greater than 2.5 GB
            {
                SortLargeFile(inputFilePath, outputFilePath);
            }
            else
            {
                SortSmallFile(inputFilePath, outputFilePath);
            }

            Console.WriteLine("File sorting completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred during file sorting:");
            Console.WriteLine(ex.Message);
        }
    }

    private static void SortLargeFile(string inputFilePath, string outputFilePath)
    {
        // Divide large file into chunks and sort them individually
        var chunkFiles = new List<string>();
        var tempDirectory = Path.Combine(Path.GetDirectoryName(outputFilePath), "Temp");

        if (!Directory.Exists(tempDirectory))
            Directory.CreateDirectory(tempDirectory);

        using (var reader = new StreamReader(inputFilePath))
        {
            int chunkNumber = 0;
            var lines = new List<string>();

            while (!reader.EndOfStream)
            {
                lines.Add(reader.ReadLine());

                if (lines.Count >= ChunkSize)
                {
                    lines.Sort();
                    var chunkFilePath = Path.Combine(tempDirectory, $"chunk{chunkNumber}.txt");

                    using (var writer = new StreamWriter(chunkFilePath))
                    {
                        foreach (var line in lines)
                        {
                            writer.WriteLine(line);
                        }
                    }

                    chunkFiles.Add(chunkFilePath);
                    lines.Clear();
                    chunkNumber++;
                }
            }

            // Sort and save the remaining lines as the last chunk
            if (lines.Count > 0)
            {
                lines.Sort();
                var chunkFilePath = Path.Combine(tempDirectory, $"chunk{chunkNumber}.txt");

                using (var writer = new StreamWriter(chunkFilePath))
                {
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }

                chunkFiles.Add(chunkFilePath);
            }
        }

        // Merge sorted chunks into the final output file
        MergeChunks(chunkFiles, outputFilePath);
        DeleteTempChunks(chunkFiles);
    }

    private static void MergeChunks(List<string> chunkFiles, string outputFilePath)
    {
        using (var outputWriter = new StreamWriter(outputFilePath))
        {
            var heap = new MinHeap<LineReader>(chunkFiles.Count);

            // Initialize the heap with the first line reader from each chunk file
            foreach (var chunkFile in chunkFiles)
            {
                var lineReader = new LineReader(chunkFile);
                if (!lineReader.EndOfStream)
                    heap.Insert(lineReader);
            }

            while (heap.Count > 0)
            {
                // Get the line reader with the smallest line
                var minLineReader = heap.RemoveMin();

                // Write the line to the output file
                outputWriter.WriteLine(minLineReader.ReadLine());

                // If the line reader has more lines, insert it back into the heap
                if (!minLineReader.EndOfStream)
                    heap.Insert(minLineReader);

                // Dispose the line reader if it has reached the end of the file
                else
                    minLineReader.Dispose();
            }
        }
    }

    private static void DeleteTempChunks(List<string> chunkFiles)
    {
        foreach (var chunkFile in chunkFiles)
        {
            File.Delete(chunkFile);
        }
    }

    private static void SortSmallFile(string inputFilePath, string outputFilePath)
    {
        var lines = File.ReadAllLines(inputFilePath).ToList();
        lines.Sort();

        File.WriteAllLines(outputFilePath, lines);
    }
}