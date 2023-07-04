using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSortingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Programe.Maind();
           
        }

        static void runner()
        {
            Console.WriteLine("Enter the path of the input file:");
            string inputFilePath = "10.txt";// Console.ReadLine();

            Console.WriteLine("Enter the name of the output file:");
            string outputFileName = "out10.txt";// Console.ReadLine();
            string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath), outputFileName);

            FileSorter.SortFile(inputFilePath, outputFilePath);

            Console.WriteLine("Sorting completed. Press any key to exit.");
            Console.ReadKey();
        }
    }

    public static class FileSorter
    {
        private const long MaxMemorySizeForInMemorySort = 2_500_000_000; // 2.5 GB
        private const int DefaultChunkSize = 100_000; // Default chunk size if estimation is not possible
        private const int NumberOfParts = 2000; // Divide the file into 10 parts initially

        public static void SortFile(string inputFilePath, string outputFilePath)
        {
            var fileSize = new FileInfo(inputFilePath).Length;

            if (fileSize <= MaxMemorySizeForInMemorySort)
            {
                SortSmallFile(inputFilePath, outputFilePath);
            }
            else
            {
                var partSize = fileSize / NumberOfParts;
                var partFiles = DivideIntoParts(inputFilePath, partSize);
                var chunkSize = GetChunkSize(partSize);

                var sortedChunks = new List<string>();

                try
                {
                    foreach (var partFile in partFiles)
                    {
                        var chunkFiles = SplitIntoChunks(partFile, chunkSize);
                        var sortedChunkFiles = SortChunks(chunkFiles);
                        sortedChunks.AddRange(sortedChunkFiles);
                    }

                    MergeSortedChunks(sortedChunks, outputFilePath);
                }
                finally
                {
                    // Delete temporary files
                    DeleteTempFiles(partFiles);
                    DeleteTempFiles(sortedChunks);
                }
            }
        }

        private static List<string> DivideIntoParts(string inputFilePath, long partSize)
        {
            var partFiles = new List<string>();

            using (var reader = new StreamReader(inputFilePath))
            {
                for (int i = 0; i < NumberOfParts; i++)
                {
                    var partFile = Path.Combine(Path.GetDirectoryName(inputFilePath), $"part_{i}.txt");
                    partFiles.Add(partFile);

                    using (var writer = new StreamWriter(partFile))
                    {
                        var fileSize = 0L;
                        string line;
                        long readLine = 0;
                        while ((line = reader.ReadLine()) != null && fileSize < partSize)
                        {
                            if (++readLine % 5000 == 0)
                                Console.Write("{0:f2}%   \r", 100.0 * reader.BaseStream.Position / reader.BaseStream.Length);
                            writer.WriteLine(line);
                            fileSize += line.Length + Environment.NewLine.Length;
                        }
                    }
                }
            }

            return partFiles;
        }

        private static int GetChunkSize(long fileSize)
        {
            var availableMemory = GC.GetTotalMemory(false);
            var availableMemoryPerLine = availableMemory / 1024; // Assume average line length of 1024 characters

            var chunkSize = (int)(MaxMemorySizeForInMemorySort / availableMemoryPerLine);
            return Math.Max(chunkSize, DefaultChunkSize);
        }

        private static List<string> SplitIntoChunks(string inputFilePath, int chunkSize)
        {
            var chunkFiles = new List<string>();

            using (var reader = new StreamReader(inputFilePath))
            {
                string line;
                int chunkNumber = 0;
                long readLine = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    var chunkLines = new List<string> { line };

                    while (chunkLines.Count < chunkSize && (line = reader.ReadLine()) != null)
                    {
                        chunkLines.Add(line);
                    }

                    chunkLines.Sort(new LineComparer());
                    
                    if (++readLine % 5000 == 0)
                        Console.Write("{0:f2}%   \r", 100.0 * reader.BaseStream.Position / reader.BaseStream.Length);

                    var chunkFile = Path.Combine(Path.GetDirectoryName(inputFilePath), $"chunk_{Path.GetFileNameWithoutExtension(inputFilePath)}_{chunkNumber}.txt");
                    File.WriteAllLines(chunkFile, chunkLines);

                    chunkFiles.Add(chunkFile);
                    chunkNumber++;
                }
            }

            return chunkFiles;
        }

        public static List<string> SortChunks(List<string> chunkFiles)
        {
            var sortedChunkFiles = new List<string>();

            foreach (var chunkFile in chunkFiles)
            {
                var lines = File.ReadAllLines(chunkFile);
                Array.Sort(lines, new LineComparer());

                var sortedChunkFile = Path.Combine(Path.GetDirectoryName(chunkFile), $"sorted_{Path.GetFileName(chunkFile)}");
                File.WriteAllLines(sortedChunkFile, lines);
                Console.WriteLine($"sorted_{Path.GetFileName(chunkFile)}");
                sortedChunkFiles.Add(sortedChunkFile);
            }

            return sortedChunkFiles;
        }

        private static void MergeSortedChunks(List<string> chunkFiles, string outputFilePath)
        {
            using (var outputWriter = new StreamWriter(outputFilePath))
            {
                var readers = new List<ChunkLineReader>();

                try
                {
                    // Initialize the line readers
                    foreach (var chunkFile in chunkFiles)
                    {
                        var reader = new ChunkLineReader(chunkFile);
                        readers.Add(reader);
                    }

                    // Create a min heap to track the minimum line from each chunk
                    var heap = new MinHeap<ChunkLineReader>(readers.Count);
                    foreach (var reader in readers)
                    {
                        heap.Insert(reader);
                    }

                    // Merge the chunks
                    while (heap.Count > 0)
                    {
                        var minLineReader = heap.RemoveMin();
                        var line = minLineReader.ReadLine();
                        outputWriter.WriteLine(line);

                        if (!minLineReader.EndOfStream)
                        {
                            heap.Insert(minLineReader);
                        }
                    }
                }
                finally
                {
                    // Dispose the line readers
                    foreach (var reader in readers)
                    {
                        reader.Dispose();
                    }
                }
            }
        }

        private static void DeleteTempFiles(List<string> files)
        {
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        private static void SortSmallFile(string inputFilePath, string outputFilePath)
        {
            var lines = File.ReadAllLines(inputFilePath);
            Array.Sort(lines, new LineComparer());

            File.WriteAllLines(outputFilePath, lines);
        }

        private class ChunkLineReader : IDisposable, IComparable<ChunkLineReader>
        {
            private const int BufferSize = 64 * 1024; // 64 KB
            private readonly StreamReader _reader;
            private string _currentLine;

            public bool EndOfStream => _reader.EndOfStream;

            public ChunkLineReader(string filePath)
            {
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _reader = new StreamReader(fileStream, leaveOpen: false, bufferSize: BufferSize);
                _currentLine = _reader.ReadLine();
            }

            public string ReadLine()
            {
                var line = _currentLine;
                _currentLine = _reader.ReadLine();
                return line;
            }

            public void Dispose()
            {
                _reader.Dispose();
            }

            public int CompareTo(ChunkLineReader other)
            {
                if (other == null) return 1;
                return string.Compare(_currentLine, other._currentLine, StringComparison.Ordinal);
            }
        }

        private class MinHeap<T> where T : IComparable<T>
        {
            private readonly T[] _items;
            private int _size;

            public int Count => _size;

            public MinHeap(int capacity)
            {
                _items = new T[capacity];
            }

            public void Insert(T item)
            {
                _items[_size] = item;
                SiftUp(_size);
                _size++;
            }

            public T RemoveMin()
            {
                var minItem = _items[0];
                _size--;
                Swap(0, _size);
                SiftDown(0);
                return minItem;
            }

            private void SiftUp(int index)
            {
                while (index > 0)
                {
                    var parentIndex = (index - 1) / 2;

                    if (_items[index].CompareTo(_items[parentIndex]) >= 0)
                        break;

                    Swap(index, parentIndex);
                    index = parentIndex;
                }
            }

            private void SiftDown(int index)
            {
                while (index * 2 + 1 < _size)
                {
                    var leftChildIndex = index * 2 + 1;
                    var rightChildIndex = index * 2 + 2;
                    var smallestChildIndex = leftChildIndex;

                    if (rightChildIndex < _size && _items[rightChildIndex].CompareTo(_items[leftChildIndex]) < 0)
                    {
                        smallestChildIndex = rightChildIndex;
                    }

                    if (_items[index].CompareTo(_items[smallestChildIndex]) <= 0)
                        break;
                    Swap(index, smallestChildIndex);
                    index = smallestChildIndex;
                }
            }

            private void Swap(int index1, int index2)
            {
                var temp = _items[index1];
                _items[index1] = _items[index2];
                _items[index2] = temp;
            }
        }

        private class LineComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                var xParts = x.Split(new[] { '.' }, 2);
                var yParts = y.Split(new[] { '.' }, 2);

                var xString = xParts[1];
                var yString = yParts[1];

                var xNumber = int.Parse(xParts[0]);
                var yNumber = int.Parse(yParts[0]);

                var stringComparison = string.Compare(xString, yString, StringComparison.Ordinal);
                if (stringComparison != 0)
                {
                    return stringComparison;
                }

                return xNumber.CompareTo(yNumber);
            }
        }
    }
}
