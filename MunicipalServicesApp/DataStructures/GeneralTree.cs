using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A node in a general (n-ary) tree: any number of children.
    /// Named GeneralTreeNode to avoid a clash with System.Windows.Forms.TreeNode.
    /// </summary>
    public class GeneralTreeNode<T>
    {
        private readonly List<GeneralTreeNode<T>> _children = new List<GeneralTreeNode<T>>();

        public T Value { get; set; }
        public GeneralTreeNode<T> Parent { get; private set; }

        public IReadOnlyList<GeneralTreeNode<T>> Children
        {
            get { return _children; }
        }

        public bool IsLeaf
        {
            get { return _children.Count == 0; }
        }

        /// <summary>Distance from the root (the root has depth 0).</summary>
        public int Depth
        {
            get
            {
                int depth = 0;
                GeneralTreeNode<T> ancestor = Parent;
                while (ancestor != null)
                {
                    depth++;
                    ancestor = ancestor.Parent;
                }
                return depth;
            }
        }

        public GeneralTreeNode(T value)
        {
            Value = value;
        }

        internal GeneralTreeNode<T> AddChild(T value)
        {
            var child = new GeneralTreeNode<T>(value);
            child.Parent = this;
            _children.Add(child);
            return child;
        }
    }

    /// <summary>
    /// A basic (general) tree. In the app it organises service requests as
    /// Municipality -> Category -> Request, which is the structure shown in the
    /// browse-by-category view.
    /// </summary>
    public class GeneralTree<T>
    {
        public GeneralTreeNode<T> Root { get; private set; }
        public int Count { get; private set; }

        public GeneralTree(T rootValue)
        {
            Root = new GeneralTreeNode<T>(rootValue);
            Count = 1;
        }

        public GeneralTreeNode<T> AddChild(GeneralTreeNode<T> parent, T value)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            Count++;
            return parent.AddChild(value);
        }

        /// <summary>Depth-first: a node, then each child subtree in order.</summary>
        public IEnumerable<GeneralTreeNode<T>> PreOrder()
        {
            var stack = new Stack<GeneralTreeNode<T>>();
            stack.Push(Root);

            while (stack.Count > 0)
            {
                GeneralTreeNode<T> node = stack.Pop();
                yield return node;

                // Push children in reverse so the first child is processed first
                for (int i = node.Children.Count - 1; i >= 0; i--)
                    stack.Push(node.Children[i]);
            }
        }

        /// <summary>Breadth-first: level by level.</summary>
        public IEnumerable<GeneralTreeNode<T>> LevelOrder()
        {
            var queue = new Queue<GeneralTreeNode<T>>();
            queue.Enqueue(Root);

            while (queue.Count > 0)
            {
                GeneralTreeNode<T> node = queue.Dequeue();
                yield return node;

                foreach (GeneralTreeNode<T> child in node.Children)
                    queue.Enqueue(child);
            }
        }

        /// <summary>Number of levels (a tree with only a root has height 1).</summary>
        public int Height
        {
            get
            {
                int height = 0;
                var level = new Queue<GeneralTreeNode<T>>();
                level.Enqueue(Root);

                while (level.Count > 0)
                {
                    height++;
                    int nodesInLevel = level.Count;
                    for (int i = 0; i < nodesInLevel; i++)
                    {
                        GeneralTreeNode<T> node = level.Dequeue();
                        foreach (GeneralTreeNode<T> child in node.Children)
                            level.Enqueue(child);
                    }
                }

                return height;
            }
        }

        /// <summary>First node (depth-first) whose value matches, or null.</summary>
        public GeneralTreeNode<T> Find(Predicate<T> match)
        {
            foreach (GeneralTreeNode<T> node in PreOrder())
            {
                if (match(node.Value))
                    return node;
            }
            return null;
        }
    }
}