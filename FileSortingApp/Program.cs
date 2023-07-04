
namespace FileSortingApp
{
    class Program
    {
        public static void Main()
        {
            Console.WriteLine("Enter the path to the input file:");
            string inputFilePath = Console.ReadLine();

            try
            {
                string outputFilePath = "sortedResult.txt";
                SortFile(inputFilePath, outputFilePath);

                Utilities.WriteWithTime("Sort completed successfully.");
                Console.WriteLine("Output file: " + outputFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during sorting: " + ex.Message);
            }

            Console.ReadLine();
        }


        public static void SortFile(string inputFilePath, string outputFilePath)
        {
            long fileSize = new FileInfo(inputFilePath).Length;

            if (fileSize > 2.5 * 1024 * 1024 * 1024) // Greater than 2.5 GB
            {
                long chunkSize = Utilities.CalculateChunkSize(fileSize);
                Sort.SortFileWithChunking(inputFilePath, outputFilePath, chunkSize);
            }
            else
            {
                Sort.SortFileInMemory(inputFilePath, outputFilePath);
            }
        }
    }
}
