namespace FileSortingApp;

public class Merge
{
    public static void MergeSortedFiles(List<string> sortedFiles, string outputFilePath)
    {
        Utilities.WriteWithTime("Merging in progress...");
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
            Utilities.WriteWithTime("Merged succesfully.");
        }
        
    }
}