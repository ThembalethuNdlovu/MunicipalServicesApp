using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A binary tree node that also remembers its colour and its parent.
    /// New nodes start red.
    /// </summary>
    internal class RbNode<TKey, TValue> : BinaryTreeNode<TKey, TValue>
    {
        public bool IsRed { get; set; }
        public RbNode<TKey, TValue> Parent { get; set; }

        public RbNode(TKey key, TValue value) : base(key, value)
        {
            IsRed = true;
        }
    }

    /// <summary>
    /// A self-balancing binary search tree that keeps itself balanced with colours
    /// instead of heights. Every node is red or black and four rules always hold:
    ///   1. The root is black.
    ///   2. A red node never has a red child.
    ///   3. Every path from a node down to an empty spot has the same number of black nodes.
    ///   4. Keys are in binary-search-tree order.
    /// Rules 2 and 3 together mean the longest path is at most twice the shortest, so the
    /// height is never more than 2 x log2(n + 1) and every search is O(log n).
    ///
    /// Compared with the AVL tree it is balanced less strictly, so lookups can need a
    /// few more comparisons, but an insert never needs more than 2 rotations. That
    /// makes it a good fit for an index that is written to all the time (new requests).
    /// This version supports insert only, which is all the date index needs.
    /// </summary>
    public class RedBlackTree<TKey, TValue> : BinaryTree<TKey, TValue>
    {
        /// <summary>Total single rotations performed since the tree was created (a double rotation counts as 2).</summary>
        public int RotationCount { get; private set; }

        /// <summary>Total times a parent, its uncle and its grandparent were recoloured during fix-up.</summary>
        public int RecolourCount { get; private set; }

        public RedBlackTree(IComparer<TKey> comparer = null) : base(comparer)
        {
        }

        public override string TreeName
        {
            get { return "Red-Black Tree (colour-balanced)"; }
        }

        /// <summary>Number of black nodes on any path from the root down (all paths are equal in a valid tree).</summary>
        public int BlackHeight
        {
            get
            {
                int blackNodes = 0;
                var node = Root as RbNode<TKey, TValue>;
                while (node != null)
                {
                    if (!node.IsRed) blackNodes++;
                    node = node.Left as RbNode<TKey, TValue>;
                }
                return blackNodes;
            }
        }

        // =====================================================================
        //  Insert
        // =====================================================================
        public override bool Insert(TKey key, TValue value)
        {
            int comparisons = 0;
            RbNode<TKey, TValue> parent = null;
            BinaryTreeNode<TKey, TValue> current = Root;
            bool goLeft = false;

            // Ordinary binary-search-tree descent to find the empty spot
            while (current != null)
            {
                comparisons++;
                int result = KeyComparer.Compare(key, current.Key);

                if (result == 0)
                {
                    LastOperationComparisons = comparisons;
                    return false; // duplicate key: nothing changes
                }

                parent = (RbNode<TKey, TValue>)current;
                goLeft = result < 0;
                current = goLeft ? current.Left : current.Right;
            }

            var node = new RbNode<TKey, TValue>(key, value);
            node.Parent = parent;

            if (parent == null)
                Root = node;
            else if (goLeft)
                parent.Left = node;
            else
                parent.Right = node;

            Count++;
            LastOperationComparisons = comparisons;

            FixAfterInsert(node);
            return true;
        }

        /// <summary>
        /// The new node is red, which can only break rule 2 (red node with a red parent)
        /// or rule 1 (a red root). Walk up and repair, looking at the new node's uncle:
        ///   - uncle red   : recolour parent, uncle and grandparent, then continue from the grandparent;
        ///   - uncle black : one or two rotations plus a recolour, and we are finished.
        /// </summary>
        private void FixAfterInsert(RbNode<TKey, TValue> node)
        {
            while (node.Parent != null && node.Parent.IsRed)
            {
                RbNode<TKey, TValue> parent = node.Parent;

                // A red parent is never the root (the root is black), so a grandparent exists
                RbNode<TKey, TValue> grandparent = parent.Parent;
                bool parentIsLeft = grandparent.Left == parent;

                RbNode<TKey, TValue> uncle = parentIsLeft
                    ? (RbNode<TKey, TValue>)grandparent.Right
                    : (RbNode<TKey, TValue>)grandparent.Left;

                if (uncle != null && uncle.IsRed)
                {
                    // Case 1: push the blackness down from the grandparent
                    parent.IsRed = false;
                    uncle.IsRed = false;
                    grandparent.IsRed = true;
                    RecolourCount++;

                    node = grandparent; // the grandparent is now red and may clash with its own parent
                }
                else
                {
                    if (parentIsLeft)
                    {
                        // Case 2 (left-right): straighten the zig-zag into a straight line
                        if (node == parent.Right)
                        {
                            RotateLeft(parent);
                            node = parent;          // old parent is now the lower node
                            parent = node.Parent;   // old node is now above it
                        }

                        // Case 3 (left-left)
                        RotateRight(grandparent);
                    }
                    else
                    {
                        // Case 2 mirrored (right-left)
                        if (node == parent.Left)
                        {
                            RotateRight(parent);
                            node = parent;
                            parent = node.Parent;
                        }

                        // Case 3 mirrored (right-right)
                        RotateLeft(grandparent);
                    }

                    // 'parent' is now the top of this little group
                    parent.IsRed = false;
                    grandparent.IsRed = true;
                    break;
                }
            }

            ((RbNode<TKey, TValue>)Root).IsRed = false; // rule 1
        }

        // =====================================================================
        //  Rotations (parent pointers are kept up to date)
        // =====================================================================
        /// <summary>
        ///     x                    y
        ///    / \                  / \
        ///   A   y      --->      x   C
        ///      / \              / \
        ///     B   C            A   B
        /// </summary>
        private void RotateLeft(RbNode<TKey, TValue> x)
        {
            var y = (RbNode<TKey, TValue>)x.Right;

            x.Right = y.Left;
            if (y.Left != null)
                ((RbNode<TKey, TValue>)y.Left).Parent = x;

            ReplaceChild(x.Parent, x, y);

            y.Left = x;
            x.Parent = y;
            RotationCount++;
        }

        /// <summary>
        ///       y                x
        ///      / \              / \
        ///     x   C    --->    A   y
        ///    / \                  / \
        ///   A   B                B   C
        /// </summary>
        private void RotateRight(RbNode<TKey, TValue> y)
        {
            var x = (RbNode<TKey, TValue>)y.Left;

            y.Left = x.Right;
            if (x.Right != null)
                ((RbNode<TKey, TValue>)x.Right).Parent = y;

            ReplaceChild(y.Parent, y, x);

            x.Right = y;
            y.Parent = x;
            RotationCount++;
        }

        /// <summary>Puts newChild where oldChild hung from parent (or makes it the root).</summary>
        private void ReplaceChild(RbNode<TKey, TValue> parent, RbNode<TKey, TValue> oldChild, RbNode<TKey, TValue> newChild)
        {
            newChild.Parent = parent;

            if (parent == null)
                Root = newChild;
            else if (parent.Left == oldChild)
                parent.Left = newChild;
            else
                parent.Right = newChild;
        }

        // =====================================================================
        //  Colour lookup, self-check and diagram
        // =====================================================================
        /// <summary>"Red" or "Black" for the node holding this key, or null if the key is not in the tree.</summary>
        public string GetColour(TKey key)
        {
            BinaryTreeNode<TKey, TValue> current = Root;

            while (current != null)
            {
                int result = KeyComparer.Compare(key, current.Key);
                if (result == 0)
                    return ((RbNode<TKey, TValue>)current).IsRed ? "Red" : "Black";

                current = result < 0 ? current.Left : current.Right;
            }

            return null;
        }

        /// <summary>
        /// True if the root is black, no red node has a red child, every path has the same
        /// number of black nodes, parent pointers are consistent and the keys are in
        /// strictly ascending order.
        /// </summary>
        public bool Validate()
        {
            if (Root == null)
                return true;

            var root = Root as RbNode<TKey, TValue>;
            if (root == null || root.IsRed || root.Parent != null)
                return false;

            if (CheckNode(root) < 0)
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

        /// <summary>Returns the number of black nodes on every path below (and including) this node, or -1 if a rule is broken.</summary>
        private int CheckNode(BinaryTreeNode<TKey, TValue> node)
        {
            if (node == null)
                return 0;

            var rb = node as RbNode<TKey, TValue>;
            if (rb == null)
                return -1;

            // Rule 2: a red node must have two black (or empty) children
            if (rb.IsRed && (IsRed(node.Left) || IsRed(node.Right)))
                return -1;

            // Parent pointers must agree with the child pointers
            var leftChild = node.Left as RbNode<TKey, TValue>;
            if (node.Left != null && (leftChild == null || leftChild.Parent != rb))
                return -1;

            var rightChild = node.Right as RbNode<TKey, TValue>;
            if (node.Right != null && (rightChild == null || rightChild.Parent != rb))
                return -1;

            int leftBlack = CheckNode(node.Left);
            if (leftBlack < 0) return -1;

            int rightBlack = CheckNode(node.Right);
            if (rightBlack < 0) return -1;

            // Rule 3: both sides must hold the same number of black nodes
            if (leftBlack != rightBlack)
                return -1;

            return leftBlack + (rb.IsRed ? 0 : 1);
        }

        private static bool IsRed(BinaryTreeNode<TKey, TValue> node)
        {
            var rb = node as RbNode<TKey, TValue>;
            return rb != null && rb.IsRed;
        }

        protected override string NodeLabel(BinaryTreeNode<TKey, TValue> node)
        {
            return base.NodeLabel(node) + (((RbNode<TKey, TValue>)node).IsRed ? " (R)" : " (B)");
        }
    }
}