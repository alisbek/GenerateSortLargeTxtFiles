namespace FileSortingApp;

public class LineReader : IDisposable, IComparable<LineReader>
{
    private readonly StreamReader _reader;

    public LineReader(string filePath)
    {
        _reader = new StreamReader(filePath);
    }

    public bool EndOfStream => _reader.EndOfStream;

    public string ReadLine()
    {
        return _reader.ReadLine();
    }

    public int CompareTo(LineReader other)
    {
        if (other == null)
            return 1;

        return ReadLine().CompareTo(other.ReadLine());
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}