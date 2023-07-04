using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSortingApp
{
    public static class FileSorter
    {
        private const long MaxMemorySizeForInMemorySort = 2_500_000_000; // 2.5 GB
        private const int DefaultChunkSize = 100_000; // Default chunk size if estimation is not possible

        public static void SortFile(string inputFilePath, string outputFilePath)
        {
            var fileSize = new FileInfo(inputFilePath).Length;
            var chunkSize = GetChunkSize(fileSize);

            if (chunkSize < fileSize)
            {
                SortLargeFile(inputFilePath, outputFilePath, chunkSize);
            }
            else
            {
                SortSmallFile(inputFilePath, outputFilePath);
            }
        }

        private static int GetChunkSize(long fileSize)
        {
            var availableMemory = GC.GetTotalMemory(false);
            var availableMemoryPerLine = availableMemory / 1024; // Assume average line length of 1024 characters

            var chunkSize = (int)(MaxMemorySizeForInMemorySort / availableMemoryPerLine);
            return Math.Max(chunkSize, DefaultChunkSize);
        }

        private static void SortLargeFile(string inputFilePath, string outputFilePath, int chunkSize)
        {
            var chunkFiles = new List<string>();

            try
            {
                // Divide the input file into smaller chunks
                using (var reader = new StreamReader(inputFilePath))
                {
                    string line;
                    int chunkNumber = 0;

                    while ((line = reader.ReadLine()) != null)
                    {
                        var chunkLines = new List<string> { line };

                        while (chunkLines.Count < chunkSize && (line = reader.ReadLine()) != null)
                        {
                            chunkLines.Add(line);
                        }

                        chunkLines.Sort(new LineComparer());

                        var chunkFile = Path.Combine(Path.GetDirectoryName(inputFilePath), $"chunk_{chunkNumber}.txt");
                        File.WriteAllLines(chunkFile, chunkLines);
                        Console.WriteLine($"chunk_{chunkNumber}.txt");
                        chunkFiles.Add(chunkFile);
                        chunkNumber++;
                    }
                }

                // Merge the sorted chunks
                MergeSortedChunks(chunkFiles, outputFilePath);
            }
            finally
            {
                // Delete the temporary chunk files
                DeleteTempChunks(chunkFiles);
            }
        }

        private static void MergeSortedChunks(List<string> chunkFiles, string outputFilePath)
        {
            var lineReaders = new List<ChunkLineReader>();
            var outputWriter = new StreamWriter(outputFilePath);

            try
            {
                // Initialize the line readers for each chunk
                foreach (var chunkFile in chunkFiles)
                {
                    lineReaders.Add(new ChunkLineReader(chunkFile));
                }

                var heap = new MinHeap<ChunkLineReader>(lineReaders.Count);

                // Insert the first line from each chunk into the heap
                foreach (var lineReader in lineReaders)
                {
                    if (!lineReader.EndOfStream)
                    {
                        heap.Insert(lineReader);
                    }
                }

                // Merge the sorted chunks
                while (heap.Count > 0)
                {
                    var minLineReader = heap.RemoveMin();

                    // Write the line to the output file
                    outputWriter.WriteLine(minLineReader.ReadLine());

                    // If the line reader has more lines, insert it back into the heap
                    if (!minLineReader.EndOfStream)
                    {
                        heap.Insert(minLineReader);
                    }
                    else
                    {
                        minLineReader.Dispose();
                    }
                }
            }
            finally
            {
                // Dispose the line readers and close the output writer
                foreach (var lineReader in lineReaders)
                {
                    lineReader.Dispose();
                }

                outputWriter.Close();
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
            var lines = File.ReadAllLines(inputFilePath);
            Array.Sort(lines, new LineComparer());

            File.WriteAllLines(outputFilePath, lines);
        }

        private class ChunkLineReader : IDisposable, IComparable<ChunkLineReader>
        {
            private const int BufferSize = 64 * 1024; // 64 KB
            private readonly StreamReader _reader;
            private string _currentLine;

            public bool EndOfStream => _currentLine == null;

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
                    var leftChild = index * 2 + 1;
                    var rightChild = index * 2 + 2;
                    var smallestChild = leftChild;

                    if (rightChild < _size && _items[rightChild].CompareTo(_items[leftChild]) < 0)
                    {
                        smallestChild = rightChild;
                    }

                    if (_items[index].CompareTo(_items[smallestChild]) <= 0)
                        break;

                    Swap(index, smallestChild);
                    index = smallestChild;
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
                var xParts = x.Split('.');
                var yParts = y.Split('.');

                var stringComparisonResult = string.Compare(xParts[1].Trim(), yParts[1].Trim(), StringComparison.OrdinalIgnoreCase);

                if (stringComparisonResult == 0)
                {
                    var xNumber = int.Parse(xParts[0].Trim());
                    var yNumber = int.Parse(yParts[0].Trim());

                    return xNumber.CompareTo(yNumber);
                }

                return stringComparisonResult;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
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
}
