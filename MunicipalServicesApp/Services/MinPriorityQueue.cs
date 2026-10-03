using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Generic binary min-heap priority queue.
    /// The item that compares as "smallest" is dequeued first.
    /// Enqueue and Dequeue are O(log n); Peek is O(1).
    /// </summary>
    public class MinPriorityQueue<T>
    {
        private readonly List<T> _heap = new List<T>();
        private readonly Comparison<T> _compare;

        public MinPriorityQueue(Comparison<T> compare)
        {
            if (compare == null) throw new ArgumentNullException("compare");
            _compare = compare;
        }

        public int Count
        {
            get { return _heap.Count; }
        }

        public void Enqueue(T item)
        {
            _heap.Add(item);
            SiftUp(_heap.Count - 1);
        }

        public T Peek()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("The priority queue is empty.");
            return _heap[0];
        }

        public T Dequeue()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("The priority queue is empty.");

            T root = _heap[0];
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);

            if (_heap.Count > 0)
                SiftDown(0);

            return root;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_compare(_heap[index], _heap[parent]) >= 0)
                    break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            int count = _heap.Count;
            while (true)
            {
                int left = 2 * index + 1;
                int right = left + 1;
                int smallest = index;

                if (left < count && _compare(_heap[left], _heap[smallest]) < 0)
                    smallest = left;
                if (right < count && _compare(_heap[right], _heap[smallest]) < 0)
                    smallest = right;

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            T temp = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = temp;
        }
    }
}