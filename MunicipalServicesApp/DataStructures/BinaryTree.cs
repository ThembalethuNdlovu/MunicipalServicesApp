using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// Base class for every binary search tree in the app (plain BST, AVL, red-black).
    /// It holds everything that does not depend on how a tree keeps itself balanced:
    /// searching, the four traversals, range queries, height and a text diagram.
    /// Each subclass only provides its own Insert.
    ///
    /// All traversals are iterative (explicit Stack / Queue) so that a degenerate,
    /// very deep tree can never cause a stack overflow.
    /// </summary>
    public abstract class BinaryTree<TKey, TValue>
    {
        protected BinaryTreeNode<TKey, TValue> Root;
        protected readonly IComparer<TKey> KeyComparer;

        protected BinaryTree(IComparer<TKey> comparer = null)
        {
            KeyComparer = comparer ?? Comparer<TKey>.Default;
        }

        /// <summary>Display name of the structure.</summary>
        public abstract string TreeName { get; }

        public int Count { get; protected set; }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        /// <summary>
        /// Number of key comparisons made by the most recent Insert or TryGetValue.
        /// Used to compare how much work each tree type needs.
        /// </summary>
        public int LastOperationComparisons { get; protected set; }

        /// <summary>
        /// Adds a key. Returns false (and changes nothing) if the key already exists.
        /// </summary>
        public abstract bool Insert(TKey key, TValue value);

        public void Clear()
        {
            Root = null;
            Count = 0;
            LastOperationComparisons = 0;
        }

        // =====================================================================
        //  Searching
        // =====================================================================
        public bool TryGetValue(TKey key, out TValue value)
        {
            int comparisons = 0;
            BinaryTreeNode<TKey, TValue> current = Root;

            while (current != null)
            {
                comparisons++;
                int result = KeyComparer.Compare(key, current.Key);

                if (result == 0)
                {
                    LastOperationComparisons = comparisons;
                    value = current.Value;
                    return true;
                }

                current = result < 0 ? current.Left : current.Right;
            }

            LastOperationComparisons = comparisons;
            value = default(TValue);
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            TValue ignored;
            return TryGetValue(key, out ignored);
        }

        public bool TryGetMin(out TKey key, out TValue value)
        {
            key = default(TKey);
            value = default(TValue);
            if (Root == null) return false;

            BinaryTreeNode<TKey, TValue> node = Root;
            while (node.Left != null) node = node.Left;

            key = node.Key;
            value = node.Value;
            return true;
        }

        public bool TryGetMax(out TKey key, out TValue value)
        {
            key = default(TKey);
            value = default(TValue);
            if (Root == null) return false;

            BinaryTreeNode<TKey, TValue> node = Root;
            while (node.Right != null) node = node.Right;

            key = node.Key;
            value = node.Value;
            return true;
        }

        // =====================================================================
        //  Height
        // =====================================================================
        /// <summary>Number of levels: 0 for an empty tree, 1 for a single node.</summary>
        public int Height
        {
            get
            {
                if (Root == null) return 0;

                int height = 0;
                var level = new Queue<BinaryTreeNode<TKey, TValue>>();
                level.Enqueue(Root);

                while (level.Count > 0)
                {
                    height++;
                    int nodesInLevel = level.Count;
                    for (int i = 0; i < nodesInLevel; i++)
                    {
                        BinaryTreeNode<TKey, TValue> node = level.Dequeue();
                        if (node.Left != null) level.Enqueue(node.Left);
                        if (node.Right != null) level.Enqueue(node.Right);
                    }
                }

                return height;
            }
        }

        // =====================================================================
        //  Traversals
        // =====================================================================

        /// <summary>Left, node, right. For a search tree this yields keys in sorted order.</summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> InOrder()
        {
            var stack = new Stack<BinaryTreeNode<TKey, TValue>>();
            BinaryTreeNode<TKey, TValue> node = Root;

            while (node != null || stack.Count > 0)
            {
                while (node != null)
                {
                    stack.Push(node);
                    node = node.Left;
                }

                node = stack.Pop();
                yield return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
                node = node.Right;
            }
        }

        /// <summary>Node, left, right.</summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> PreOrder()
        {
            if (Root == null) yield break;

            var stack = new Stack<BinaryTreeNode<TKey, TValue>>();
            stack.Push(Root);

            while (stack.Count > 0)
            {
                BinaryTreeNode<TKey, TValue> node = stack.Pop();
                yield return new KeyValuePair<TKey, TValue>(node.Key, node.Value);

                // Push right first so that left is processed first
                if (node.Right != null) stack.Push(node.Right);
                if (node.Left != null) stack.Push(node.Left);
            }
        }

        /// <summary>Left, right, node.</summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> PostOrder()
        {
            if (Root == null) yield break;

            // Two-stack method: the second stack ends up holding the nodes in reverse post-order
            var first = new Stack<BinaryTreeNode<TKey, TValue>>();
            var second = new Stack<BinaryTreeNode<TKey, TValue>>();
            first.Push(Root);

            while (first.Count > 0)
            {
                BinaryTreeNode<TKey, TValue> node = first.Pop();
                second.Push(node);

                if (node.Left != null) first.Push(node.Left);
                if (node.Right != null) first.Push(node.Right);
            }

            while (second.Count > 0)
            {
                BinaryTreeNode<TKey, TValue> node = second.Pop();
                yield return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
            }
        }

        /// <summary>Level by level, top to bottom (breadth-first).</summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> LevelOrder()
        {
            if (Root == null) yield break;

            var queue = new Queue<BinaryTreeNode<TKey, TValue>>();
            queue.Enqueue(Root);

            while (queue.Count > 0)
            {
                BinaryTreeNode<TKey, TValue> node = queue.Dequeue();
                yield return new KeyValuePair<TKey, TValue>(node.Key, node.Value);

                if (node.Left != null) queue.Enqueue(node.Left);
                if (node.Right != null) queue.Enqueue(node.Right);
            }
        }

        // =====================================================================
        //  Range query
        // =====================================================================
        /// <summary>
        /// All entries with low &lt;= key &lt;= high, in sorted order. Whole subtrees that
        /// cannot hold a match are skipped, so this costs O(height + matches), not O(n).
        /// </summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> GetRange(TKey low, TKey high)
        {
            if (KeyComparer.Compare(low, high) > 0) yield break;

            var stack = new Stack<BinaryTreeNode<TKey, TValue>>();
            BinaryTreeNode<TKey, TValue> node = Root;

            while (node != null || stack.Count > 0)
            {
                while (node != null)
                {
                    stack.Push(node);
                    // Only descend left if this node's key is above 'low';
                    // otherwise everything on the left is too small.
                    node = KeyComparer.Compare(node.Key, low) > 0 ? node.Left : null;
                }

                node = stack.Pop();

                // In-order visits keys in ascending order, so once we pass 'high' we are done
                if (KeyComparer.Compare(node.Key, high) > 0) yield break;

                if (KeyComparer.Compare(node.Key, low) >= 0)
                    yield return new KeyValuePair<TKey, TValue>(node.Key, node.Value);

                node = node.Right;
            }
        }

        // =====================================================================
        //  Text diagram
        // =====================================================================
        /// <summary>Text for one node in the diagram. Subclasses override this to add colour / height.</summary>
        protected virtual string NodeLabel(BinaryTreeNode<TKey, TValue> node)
        {
            return node.Key == null ? "" : node.Key.ToString();
        }

        /// <summary>
        /// The tree turned on its side: the root is at the left, right children are
        /// drawn above their parent and left children below it, indented by depth.
        /// </summary>
        public List<string> ToDiagramLines()
        {
            var lines = new List<string>();
            if (Root == null) return lines;

            // Reverse in-order (right, node, left) with the depth remembered per node
            var stack = new Stack<KeyValuePair<BinaryTreeNode<TKey, TValue>, int>>();
            BinaryTreeNode<TKey, TValue> node = Root;
            int depth = 0;

            while (node != null || stack.Count > 0)
            {
                while (node != null)
                {
                    stack.Push(new KeyValuePair<BinaryTreeNode<TKey, TValue>, int>(node, depth));
                    node = node.Right;
                    depth++;
                }

                KeyValuePair<BinaryTreeNode<TKey, TValue>, int> top = stack.Pop();
                lines.Add(new string(' ', top.Value * 6) + NodeLabel(top.Key));

                node = top.Key.Left;
                depth = top.Value + 1;
            }

            return lines;
        }
    }
}