namespace Hylasoft.Opc.Common
{
    /// <summary>
    /// Base class representing a node on the OPC server
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// Gets the displayed name of the node
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the dot-separated fully qualified tag of the node
        /// </summary>
        public string Tag { get; protected set; }

        /// <summary>
        /// Gets the parent node. If the node is root, returns null
        /// </summary>
        public Node Parent { get; private set; }

        /// <summary>
        /// Creates a new node
        /// </summary>
        /// <param name="name">the name of the node</param>
        /// <param name="parent">The parent node</param>
        protected Node(string name, Node parent = null)
        {
            this.Name = name;
            this.Parent = parent;
            if (parent != null && !string.IsNullOrEmpty(parent.Tag))
                this.Tag = parent.Tag + '.' + name;
            else
                this.Tag = name;
        }

        /// <summary>
        /// Overrides ToString()
        /// </summary>
        public override string ToString()
        {
            return this.Tag;
        }
    }
}
