namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A node in a binary tree. The AVL and red-black trees derive from this
    /// to add their own balancing data (height / colour).
    /// </summary>
    public class BinaryTreeNode<TKey, TValue>
    {
        public TKey Key { get; internal set; }
        public TValue Value { get; internal set; }
        public BinaryTreeNode<TKey, TValue> Left { get; internal set; }
        public BinaryTreeNode<TKey, TValue> Right { get; internal set; }

        public BinaryTreeNode(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public bool IsLeaf
        {
            get { return Left == null && Right == null; }
        }
    }
}