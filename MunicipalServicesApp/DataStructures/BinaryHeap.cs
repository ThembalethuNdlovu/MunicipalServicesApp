using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A binary heap stored in a List (the "array" form of a complete binary tree).
    /// The item that compares as smallest is always at index 0, so Peek is O(1).
    ///
    /// For the item at index i:  parent = (i - 1) / 2,  left child = 2i + 1,  right child = 2i + 2.
    /// The heap rule: no item is smaller than its parent. Siblings are NOT ordered,
    /// which is why a heap is cheaper to maintain than a sorted list or a search tree.
    ///
    ///   Enqueue : O(log n)  (add at the end, sift up)
    ///   Dequeue : O(log n)  (move the last item to the top, sift down)
    ///   Peek    : O(1)
    ///   BuildHeap : O(n)    (heapify from the middle of the array upwards)
    ///
    /// For a max-first queue, pass a comparison that is reversed.
    /// </summary>
    public class BinaryHeap<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly Comparison<T> _compare;

        public BinaryHeap(Comparison<T> compare)
        {
            if (compare == null) throw new ArgumentNullException("compare");
            _compare = compare;
        }

        public BinaryHeap(IComparer<T> comparer)
        {
            if (comparer == null) throw new ArgumentNullException("comparer");
            _compare = comparer.Compare;
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsEmpty
        {
            get { return _items.Count == 0; }
        }

        /// <summary>Number of levels: 0 for an empty heap, 1 for a single item.</summary>
        public int Height
        {
            get
            {
                int levels = 0;
                int remaining = _items.Count;
                while (remaining > 0)
                {
                    levels++;
                    remaining /= 2;
                }
                return levels;
            }
        }

        /// <summary>Comparisons made by the most recent Enqueue, Dequeue or BuildHeap.</summary>
        public int LastOperationComparisons { get; private set; }

        /// <summary>Comparisons made since the heap was created.</summary>
        public int TotalComparisons { get; private set; }

        /// <summary>Swaps made since the heap was created.</summary>
        public int SwapCount { get; private set; }

        // =====================================================================
        //  Adding and removing
        // =====================================================================
        public void Enqueue(T item)
        {
            LastOperationComparisons = 0;
            _items.Add(item);
            SiftUp(_items.Count - 1);
        }

        /// <summary>The item that would be dequeued next. Throws if the heap is empty.</summary>
        public T Peek()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("The priority queue is empty.");
            return _items[0];
        }

        public bool TryPeek(out T item)
        {
            if (_items.Count == 0)
            {
                item = default(T);
                return false;
            }

            item = _items[0];
            return true;
        }

        /// <summary>Removes and returns the smallest item. Throws if the heap is empty.</summary>
        public T Dequeue()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("The priority queue is empty.");

            LastOperationComparisons = 0;

            T root = _items[0];
            int last = _items.Count - 1;
            _items[0] = _items[last];
            _items.RemoveAt(last);

            if (_items.Count > 0)
                SiftDown(0);

            return root;
        }

        public bool TryDequeue(out T item)
        {
            if (_items.Count == 0)
            {
                item = default(T);
                return false;
            }

            item = Dequeue();
            return true;
        }

        /// <summary>Empties the heap. The comparison and swap counters are kept.</summary>
        public void Clear()
        {
            _items.Clear();
            LastOperationComparisons = 0;
        }

        /// <summary>
        /// Replaces the contents with the given items and turns them into a heap in O(n)
        /// by sifting down every item that has children, starting from the last of them.
        /// This is cheaper than calling Enqueue n times (which is O(n log n) in the worst case).
        /// </summary>
        public void BuildHeap(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");

            _items.Clear();
            _items.AddRange(items);
            LastOperationComparisons = 0;

            for (int i = _items.Count / 2 - 1; i >= 0; i--)
                SiftDown(i);
        }

        // =====================================================================
        //  Inspecting
        // =====================================================================
        /// <summary>A copy of the items in heap (array) order. Only the first item is guaranteed to be the smallest.</summary>
        public List<T> ToList()
        {
            return new List<T>(_items);
        }

        /// <summary>
        /// All items in sorted order (smallest first) without changing this heap.
        /// It works on a copy and removes items one by one, which is exactly heap sort.
        /// </summary>
        public List<T> GetSortedCopy()
        {
            var copy = new BinaryHeap<T>(_compare);
            copy._items.AddRange(_items); // already a valid heap, so no re-ordering is needed

            var sorted = new List<T>(_items.Count);
            while (copy.Count > 0)
                sorted.Add(copy.Dequeue());

            return sorted;
        }

        /// <summary>One line per level of the heap, e.g. "Level 2: 20, 60".</summary>
        public List<string> ToLevelLines(Func<T, string> label = null)
        {
            var lines = new List<string>();
            int start = 0;
            int size = 1;
            int level = 1;

            while (start < _items.Count)
            {
                var parts = new List<string>();
                int end = Math.Min(start + size, _items.Count);

                for (int i = start; i < end; i++)
                    parts.Add(label != null ? label(_items[i]) : Convert.ToString(_items[i]));

                lines.Add("Level " + level + ": " + string.Join(", ", parts));

                start = end;
                size *= 2;
                level++;
            }

            return lines;
        }

        /// <summary>True if no item is smaller than its parent. Does not affect the comparison counters.</summary>
        public bool Validate()
        {
            for (int i = 1; i < _items.Count; i++)
            {
                int parent = (i - 1) / 2;
                if (_compare(_items[i], _items[parent]) < 0)
                    return false;
            }

            return true;
        }

        // =====================================================================
        //  Restoring the heap rule
        // =====================================================================
        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (Compare(_items[index], _items[parent]) >= 0)
                    break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            int count = _items.Count;
            while (true)
            {
                int left = 2 * index + 1;
                int right = left + 1;
                int smallest = index;

                if (left < count && Compare(_items[left], _items[smallest]) < 0)
                    smallest = left;
                if (right < count && Compare(_items[right], _items[smallest]) < 0)
                    smallest = right;

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private int Compare(T a, T b)
        {
            LastOperationComparisons++;
            TotalComparisons++;
            return _compare(a, b);
        }

        private void Swap(int a, int b)
        {
            T temp = _items[a];
            _items[a] = _items[b];
            _items[b] = temp;
            SwapCount++;
        }
    }
}