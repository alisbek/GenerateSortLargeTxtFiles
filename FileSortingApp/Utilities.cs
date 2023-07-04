namespace FileSortingApp;

public class Utilities
{
    public static List<long> CalculateChunkSizes(long fileSize, long targetChunkSize)
    {
        const int minChunkSize = 1024 * 1024;
        var chunkSizes = new List<long>();
        long remainingBytes = fileSize;
        int chunkCount = (int)Math.Ceiling((double)fileSize / targetChunkSize);

        if (chunkCount == 1)
        {
            chunkSizes.Add((int)fileSize);
        }
        else
        {
            int averageChunkSize = (int)Math.Ceiling((double)fileSize / chunkCount);
            long chunkSize = Math.Max(minChunkSize, averageChunkSize);
            remainingBytes -= chunkSize;
            chunkSizes.Add(chunkSize);

            while (chunkSizes.Count < chunkCount - 1)
            {
                chunkSize = Math.Min(chunkSize, remainingBytes);
                remainingBytes -= chunkSize;
                chunkSizes.Add(chunkSize);
            }

            if (remainingBytes > 0)
                chunkSizes.Add((int)remainingBytes);
        }

        return chunkSizes;
    }

    public static long CalculateChunkSize(long fileSize)
    {
        const int minChunkSize = 64 * 1024; 
        const int maxChunkSize = 10 * 1024 * 1024; 

        long availableMemory = GC.GetTotalMemory(false);
        long availableMemoryForFile = availableMemory / 2; 

        long desiredChunkSize = fileSize / 100;

        int chunkSize = (int)Math.Min(Math.Max(desiredChunkSize, minChunkSize), maxChunkSize);
        while (chunkSize > availableMemoryForFile)
        {
            chunkSize /= 2;
        }

        return chunkSize;
    }

    public static void WriteWithTime(string s)
    {
        Console.WriteLine("{0}: {1}", DateTime.Now.ToLongTimeString(), s);
    }
}