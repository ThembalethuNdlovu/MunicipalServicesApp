using System;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// Union-find: tracks which items belong to the same group.
    /// Kruskal's algorithm uses it to ask "would this road close a loop?" (are both ends
    /// already in the same group?) and to merge groups when a road is accepted.
    ///
    /// Two tricks keep it almost O(1) per operation:
    ///   - path compression: Find re-points every item on the way up straight at the root;
    ///   - union by rank: the shorter group is attached under the taller one.
    /// Items are the numbers 0 .. size - 1.
    /// </summary>
    public class DisjointSet
    {
        private readonly int[] _parent;
        private readonly int[] _rank;

        /// <summary>Number of separate groups that remain.</summary>
        public int SetCount { get; private set; }

        public DisjointSet(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException("size");

            _parent = new int[size];
            _rank = new int[size];
            for (int i = 0; i < size; i++)
                _parent[i] = i; // every item starts in a group of its own

            SetCount = size;
        }

        /// <summary>The representative (root) of the group the item belongs to.</summary>
        public int Find(int item)
        {
            if (item < 0 || item >= _parent.Length) throw new ArgumentOutOfRangeException("item");

            int root = item;
            while (_parent[root] != root)
                root = _parent[root];

            // Path compression
            while (_parent[item] != root)
            {
                int next = _parent[item];
                _parent[item] = root;
                item = next;
            }

            return root;
        }

        public bool Connected(int a, int b)
        {
            return Find(a) == Find(b);
        }

        /// <summary>Merges the groups of a and b. Returns false if they were already in the same group.</summary>
        public bool Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA == rootB)
                return false;

            if (_rank[rootA] < _rank[rootB])
            {
                _parent[rootA] = rootB;
            }
            else if (_rank[rootA] > _rank[rootB])
            {
                _parent[rootB] = rootA;
            }
            else
            {
                _parent[rootB] = rootA;
                _rank[rootA]++;
            }

            SetCount--;
            return true;
        }
    }
}