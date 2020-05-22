using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeGroup
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
                    node.SpreadRemadeGroup();
                }
            }
            //This should now be GC-able
        }
    }
}
