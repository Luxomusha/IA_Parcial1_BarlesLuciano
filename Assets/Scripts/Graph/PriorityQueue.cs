using System.Collections.Generic;

public class PriorityQueue<T>
{
    private readonly List<(T item, float priority)> heap = new();

    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add((item, priority));
        SiftUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        T top = heap[0].item;
        int last = heap.Count - 1;

        heap[0] = heap[last];
        heap.RemoveAt(last);

        if (heap.Count > 0)
            SiftDown(0);

        return top;
    }

    public void Clear()
    {
        heap.Clear();
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;

            if (heap[index].priority >= heap[parent].priority)
                return;

            Swap(index, parent);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            int smallest = index;
            int left = index * 2 + 1;
            int right = left + 1;

            if (left < heap.Count && heap[left].priority < heap[smallest].priority)
                smallest = left;

            if (right < heap.Count && heap[right].priority < heap[smallest].priority)
                smallest = right;

            if (smallest == index)
                return;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (heap[a], heap[b]) = (heap[b], heap[a]);
    }
}
