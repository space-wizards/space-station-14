using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    //todo: add virtual methods for power network to override

    /// <summary>
    ///     Maintains a collection of <see cref="INode"/>s, and performs operations requiring a list of
    ///     all connected <see cref="INode"/>s.
    /// </summary>
    public interface INodeGroup
    {
        public IReadOnlyList<INode> Nodes { get; }

        void AddNode(INode node);

        void RemoveNode(INode node);

        void CombineGroup(INodeGroup group);

        /// <summary>
        ///     Causes all <see cref="INode"/>s to remake their groups. Called when a <see cref="INode"/> is removed
        ///     and may have split a group in two, so multiple new groups may need to be formed.
        /// </summary>
        void RemakeGroup();
    }

    public abstract class NodeGroup : INodeGroup
    {
        [ViewVariables]
        public IReadOnlyList<INode> Nodes => _nodes;
        private readonly List<INode> _nodes = new List<INode>();

        [ViewVariables]
        public int NodeCount => Nodes.Count;

        public void AddNode(INode node)
        {
            _nodes.Add(node);
        }

        public void RemoveNode(INode node)
        {
            _nodes.Remove(node);
            RemakeGroup(); //might want to move this into a strategy on INodes so remaking is optional on node removal
        }

        public void CombineGroup(INodeGroup newGroup)
        {
            foreach (var node in Nodes)
            {
                node.NodeGroup = newGroup;
            }
            //This should now be GC-able
        }

        // <inheritdoc cref="INodeGroup"/>
        public void RemakeGroup()
        {
            foreach (var node in Nodes)
            {
                node.NodeGroup = null;
            }
            foreach (var node in Nodes)
            {
                if (node.TryAssignGroupIfNeeded())
                {
                    node.SpreadGroup();
                }
            }
            //This should now be GC-able
        }
    }
}
