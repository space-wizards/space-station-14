using System;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Associates a <see cref="Node"/> implementation with a string. This is used
    ///     to specify an <see cref="Node"/>'s strategy in yaml. Used by <see cref="INodeStateFactory"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class
        NodeStateAttribute : Attribute
    {
        public NodeStateID NodeStateID { get; }

        public NodeStateAttribute(NodeStateID nodeStateID)
        {
            NodeStateID = nodeStateID;
        }
    }
}
