using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>A binary tree node that also remembers its own height.</summary>
    internal class AvlNode<TKey, TValue> : BinaryTreeNode<TKey, TValue>
    {
        /// <summary>Levels in the subtree below and including this node (a leaf has height 1).</summary>
        public int Height { get; set; }

        public AvlNode(TKey key, TValue value) : base(key, value)
        {
            Height = 1;
        }
    }

    /// <summary>
    /// A self-balancing binary search tree (Adelson-Velsky and Landis).
    /// After every insert it checks each node on the path back to the root. If the
    /// heights of a node's two subtrees differ by more than 1, it rotates to restore
    /// balance. Height therefore stays at about 1.44 x log2(n), even when keys arrive
    /// in sorted order, so every search is O(log n) in the worst case.
    /// </summary>
    public class AvlTree<TKey, TValue> : BinaryTree<TKey, TValue>
    {
        // Working fields for the insert that is currently running
        private bool _inserted;
        private int _comparisons;

        /// <summary>Total single rotations performed since the tree was created (a double rotation counts as 2).</summary>
        public int RotationCount { get; private set; }

        public AvlTree(IComparer<TKey> comparer = null) : base(comparer)
        {
        }

        public override string TreeName
        {
            get { return "AVL Tree (height-balanced)"; }
        }

        public override bool Insert(TKey key, TValue value)
        {
            _inserted = false;
            _comparisons = 0;

            Root = InsertNode(Root, key, value);

            LastOperationComparisons = _comparisons;
            if (_inserted)
                Count++;

            return _inserted;
        }

        // =====================================================================
        //  Insert with rebalancing (recursive: depth is only O(log n))
        // =====================================================================
        private BinaryTreeNode<TKey, TValue> InsertNode(BinaryTreeNode<TKey, TValue> node, TKey key, TValue value)
        {
            if (node == null)
            {
                _inserted = true;
                return new AvlNode<TKey, TValue>(key, value);
            }

            _comparisons++;
            int result = KeyComparer.Compare(key, node.Key);

            if (result == 0)
                return node; // duplicate key: nothing changes

            if (result < 0)
                node.Left = InsertNode(node.Left, key, value);
            else
                node.Right = InsertNode(node.Right, key, value);

            if (!_inserted)
                return node; // duplicate found further down: no heights changed

            return Rebalance(node);
        }

        private BinaryTreeNode<TKey, TValue> Rebalance(BinaryTreeNode<TKey, TValue> node)
        {
            UpdateHeight(node);
            int balance = BalanceFactor(node);

            // Left side too tall
            if (balance > 1)
            {
                // Left-right case: first straighten the left child with a left rotation
                if (BalanceFactor(node.Left) < 0)
                    node.Left = RotateLeft(node.Left);

                return RotateRight(node);
            }

            // Right side too tall
            if (balance < -1)
            {
                // Right-left case: first straighten the right child with a right rotation
                if (BalanceFactor(node.Right) > 0)
                    node.Right = RotateRight(node.Right);

                return RotateLeft(node);
            }

            return node; // already balanced
        }

        /// <summary>
        ///       y                x
        ///      / \              / \
        ///     x   C    --->    A   y
        ///    / \                  / \
        ///   A   B                B   C
        /// </summary>
        private BinaryTreeNode<TKey, TValue> RotateRight(BinaryTreeNode<TKey, TValue> y)
        {
            BinaryTreeNode<TKey, TValue> x = y.Left;
            BinaryTreeNode<TKey, TValue> b = x.Right;

            x.Right = y;
            y.Left = b;

            UpdateHeight(y); // y is now the lower node, so update it first
            UpdateHeight(x);
            RotationCount++;
            return x;
        }

        /// <summary>
        ///     x                    y
        ///    / \                  / \
        ///   A   y      --->      x   C
        ///      / \              / \
        ///     B   C            A   B
        /// </summary>
        private BinaryTreeNode<TKey, TValue> RotateLeft(BinaryTreeNode<TKey, TValue> x)
        {
            BinaryTreeNode<TKey, TValue> y = x.Right;
            BinaryTreeNode<TKey, TValue> b = y.Left;

            y.Left = x;
            x.Right = b;

            UpdateHeight(x);
            UpdateHeight(y);
            RotationCount++;
            return y;
        }

        // =====================================================================
        //  Height helpers
        // =====================================================================
        private static int HeightOf(BinaryTreeNode<TKey, TValue> node)
        {
            return node == null ? 0 : ((AvlNode<TKey, TValue>)node).Height;
        }

        private static void UpdateHeight(BinaryTreeNode<TKey, TValue> node)
        {
            ((AvlNode<TKey, TValue>)node).Height =
                Math.Max(HeightOf(node.Left), HeightOf(node.Right)) + 1;
        }

        /// <summary>Positive = left side taller, negative = right side taller.</summary>
        private static int BalanceFactor(BinaryTreeNode<TKey, TValue> node)
        {
            return node == null ? 0 : HeightOf(node.Left) - HeightOf(node.Right);
        }

        // =====================================================================
        //  Self-check and diagram
        // =====================================================================
        /// <summary>
        /// True if every stored height is correct, every balance factor is -1, 0 or +1,
        /// and the keys are in strictly ascending order.
        /// </summary>
        public bool Validate()
        {
            if (Root == null)
                return true;

            if (CheckNode(Root) < 0)
                return false;

            bool first = true;
            TKey previous = default(TKey);
            foreach (KeyValuePair<TKey, TValue> pair in InOrder())
            {
                if (!first && KeyComparer.Compare(previous, pair.Key) >= 0)
                    return false;

                previous = pair.Key;
                first = false;
            }

            return true;
        }

        /// <summary>Returns the subtree's true height, or -1 if any rule is broken below it.</summary>
        private int CheckNode(BinaryTreeNode<TKey, TValue> node)
        {
            if (node == null)
                return 0;

            var avlNode = node as AvlNode<TKey, TValue>;
            if (avlNode == null)
                return -1;

            int leftHeight = CheckNode(node.Left);
            if (leftHeight < 0) return -1;

            int rightHeight = CheckNode(node.Right);
            if (rightHeight < 0) return -1;

            if (Math.Abs(leftHeight - rightHeight) > 1)
                return -1;

            int height = Math.Max(leftHeight, rightHeight) + 1;
            return avlNode.Height == height ? height : -1;
        }

        protected override string NodeLabel(BinaryTreeNode<TKey, TValue> node)
        {
            return base.NodeLabel(node) + " (h" + ((AvlNode<TKey, TValue>)node).Height + ")";
        }
    }
}