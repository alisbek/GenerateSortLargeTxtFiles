namespace FileSortingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the path to the input file:");
            string inputFilePath = Console.ReadLine();

            Console.WriteLine("Enter the path to the output file:");
            string outputFilePath = Console.ReadLine();

            try
            {
                FileInfo inputFile = new FileInfo(inputFilePath);
                long fileSize = inputFile.Length;

                if (fileSize > 2.5 * 1024 * 1024 * 1024)
                {
                    // Sort large file using chunking
                    int chunkSize = CalculateChunkSize(fileSize);
                    SortLargeFile(inputFilePath, outputFilePath, chunkSize);
                }
                else
                {
                    // Sort small file in memory
                    SortSmallFile(inputFilePath, outputFilePath);
                }

                Console.WriteLine("Sorting completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during sorting:");
                Console.WriteLine(ex.Message);
            }
        }

        static int CalculateChunkSize(long fileSize)
        {
            // Calculate the chunk size dynamically based on the input file size
            const long maxChunkSize = 2L * 1024 * 1024 * 1024; // 2 GB

            int chunkSize = (int)(fileSize / (Math.Ceiling((double)fileSize / maxChunkSize)));
            return chunkSize;
        }

        static void SortLargeFile(string inputFilePath, string outputFilePath, int chunkSize)
        {
            // Read the input file in chunks
            List<string> lines = new List<string>();

            using (StreamReader reader = new StreamReader(inputFilePath))
            {
                char[] buffer = new char[chunkSize];
                int bytesRead;

                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string chunk = new string(buffer, 0, bytesRead);
                    lines.AddRange(chunk.Split('\n'));
                }
            }

            // Sort the lines in parallel using custom comparer
            lines.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .OrderBy(line => line, new CustomComparer())
                .ForAll(Console.WriteLine);

            // Write the sorted lines to the output file
            File.WriteAllLines(outputFilePath, lines);
        }

        static void SortSmallFile(string inputFilePath, string outputFilePath)
        {
            // Read the input file into memory
            List<string> lines = File.ReadLines(inputFilePath).ToList();

            // Sort the lines using custom comparer
            lines.Sort(new CustomComparer());

            // Write the sorted lines to the output file
            File.WriteAllLines(outputFilePath, lines);
        }
    }

    class CustomComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            string[] partsX = x.Split('.');
            string[] partsY = y.Split('.');

            int result = string.Compare(partsX[1], partsY[1], StringComparison.Ordinal);
            if (result == 0)
            {
                int numberX = int.Parse(partsX[0]);
                int numberY = int.Parse(partsY[0]);
                result = numberX.CompareTo(numberY);
            }

            return result;
        }
    }
}
