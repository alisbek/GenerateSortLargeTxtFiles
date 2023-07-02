namespace FileSortingApp;

public class MinHeap<T> where T : IComparable<T>
{
    private readonly T[] _elements;
    private int _size;

    public MinHeap(int capacity)
    {
        _elements = new T[capacity];
        _size = 0;
    }

    public int Count => _size;

    public void Insert(T element)
    {
        if (_size == _elements.Length)
            throw new InvalidOperationException("Heap is full");

        _elements[_size] = element;
        SiftUp(_size);
        _size++;
    }

    public T RemoveMin()
    {
        if (_size == 0)
            throw new InvalidOperationException("Heap is empty");

        var minElement = _elements[0];
        _size--;
        _elements[0] = _elements[_size];
        SiftDown(0);

        return minElement;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if (_elements[index].CompareTo(_elements[parentIndex]) >= 0)
                break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void SiftDown(int index)
    {
        while (index < _size / 2)
        {
            int leftChildIndex = 2 * index + 1;
            int rightChildIndex = 2 * index + 2;
            int smallerChildIndex = leftChildIndex;

            if (rightChildIndex < _size && _elements[rightChildIndex].CompareTo(_elements[leftChildIndex]) < 0)
                smallerChildIndex = rightChildIndex;

            if (_elements[index].CompareTo(_elements[smallerChildIndex]) <= 0)
                break;

            Swap(index, smallerChildIndex);
            index = smallerChildIndex;
        }
    }

    private void Swap(int index1, int index2)
    {
        T temp = _elements[index1];
        _elements[index1] = _elements[index2];
        _elements[index2] = temp;
    }
}