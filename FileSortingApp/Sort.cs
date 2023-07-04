namespace FileSortingApp;

public class Sort
{
    public static void SortFileWithChunking(string inputFilePath, string outputFilePath, long chunkSize)
    {
        var tempFilePaths = new List<string>();
        var chunkSizes = Utilities.CalculateChunkSizes(new FileInfo(inputFilePath).Length, chunkSize);

        try
        {
            Split.SplitFileIntoChunks(inputFilePath, tempFilePaths, chunkSizes);
            Utilities.WriteWithTime("Sort in progress...");
            Parallel.ForEach(tempFilePaths, (tempFilePath) =>
            {
                SortFileInMemory(tempFilePath, tempFilePath);
            });
            Utilities.WriteWithTime("Chunked files sorting done.");
            Merge.MergeSortedFiles(tempFilePaths, outputFilePath);
        }
        finally
        {
            foreach (var tempFilePath in tempFilePaths)
            {
                File.Delete(tempFilePath);
            }
        }
    }

    public static void SortFileInMemory(string inputFilePath, string outputFilePath)
    {
        var lines = new List<string>();
        long readLine = 0;
        using (var inputFileStream = File.OpenRead(inputFilePath))
        using (var streamReader = new StreamReader(inputFileStream))
        {
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
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
}