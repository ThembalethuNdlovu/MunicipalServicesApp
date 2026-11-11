using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A plain binary search tree: smaller keys go left, larger keys go right.
    /// It does NOT rebalance itself, so its shape depends on the insertion order:
    ///   - random order      -> roughly balanced, searches take about O(log n)
    ///   - sorted order      -> degenerates into a linked list, searches take O(n)
    /// The AVL tree (Step 3) and red-black tree (Step 4) fix that weakness.
    /// </summary>
    public class BinarySearchTree<TKey, TValue> : BinaryTree<TKey, TValue>
    {
        public BinarySearchTree(IComparer<TKey> comparer = null) : base(comparer)
        {
        }

        public override string TreeName
        {
            get { return "Binary Search Tree (unbalanced)"; }
        }

        public override bool Insert(TKey key, TValue value)
        {
            int comparisons = 0;

            if (Root == null)
            {
                Root = new BinaryTreeNode<TKey, TValue>(key, value);
                Count = 1;
                LastOperationComparisons = 0;
                return true;
            }

            BinaryTreeNode<TKey, TValue> current = Root;

            while (true)
            {
                comparisons++;
                int result = KeyComparer.Compare(key, current.Key);

                if (result == 0)
                {
                    LastOperationComparisons = comparisons;
                    return false; // duplicate key: leave the tree unchanged
                }

                if (result < 0)
                {
                    if (current.Left == null)
                    {
                        current.Left = new BinaryTreeNode<TKey, TValue>(key, value);
                        break;
                    }
                    current = current.Left;
                }
                else
                {
                    if (current.Right == null)
                    {
                        current.Right = new BinaryTreeNode<TKey, TValue>(key, value);
                        break;
                    }
                    current = current.Right;
                }
            }

            Count++;
            LastOperationComparisons = comparisons;
            return true;
        }
    }
}