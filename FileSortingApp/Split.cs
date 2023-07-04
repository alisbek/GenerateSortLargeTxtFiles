namespace FileSortingApp;

public class Split
{
    public static void SplitFileIntoChunks(string inputFilePath, List<string> tempFilePaths, List<long> chunkSizes)
    {
        using (var inputFileStream = File.OpenRead(inputFilePath))
        {
            Utilities.WriteWithTime("Splitting file into chunks...");
            long reader = 0;
            foreach (var chunkSize in chunkSizes)
            {


                var tempFilePath = Path.GetTempFileName();
                tempFilePaths.Add(tempFilePath);

                using (var tempFileStream = File.OpenWrite(tempFilePath))
                {
                    var buffer = new byte[chunkSize];
                    int bytesRead;
                    long totalBytesRead = 0;

                    while ((bytesRead = inputFileStream.Read(buffer, 0, buffer.Length)) > 0 && totalBytesRead < chunkSize)
                    {
                        tempFileStream.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    }
                }
            }
            Utilities.WriteWithTime($"File has been chunked into {tempFilePaths.Count} parts");
        }
    }
}