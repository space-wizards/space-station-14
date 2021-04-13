#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    /// <summary>
    ///     Maintains a collection of <see cref="Node"/>s, and performs operations requiring a list of
    ///     all connected <see cref="Node"/>s.
    /// </summary>
    public interface INodeGroup
    {
        IReadOnlyList<Node> Nodes { get; }

        void Initialize(Node sourceNode);

        void AddNode(Node node);

        void RemoveNode(Node node);

        void CombineGroup(INodeGroup newGroup);

        void RemakeGroup();
    }

    [NodeGroup(NodeGroupID.Default, NodeGroupID.WireNet)]
    public class BaseNodeGroup : INodeGroup
    {
        [ViewVariables]
        public IReadOnlyList<Node> Nodes => _nodes;
        private readonly List<Node> _nodes = new();

        [ViewVariables]
        public int NodeCount => Nodes.Count;

        /// <summary>
        ///     Debug variable to indicate that this NodeGroup should not be being used by anything.
        /// </summary>
        [ViewVariables]
        public bool Removed { get; private set; } = false;

        public static readonly INodeGroup NullGroup = new NullNodeGroup();

        protected GridId GridId { get; private set;}

        public virtual void Initialize(Node sourceNode)
        {
            GridId = sourceNode.Owner.Transform.GridID;
        }

        public void AddNode(Node node)
        {
            _nodes.Add(node);
            OnAddNode(node);
        }

        public void RemoveNode(Node node)
        {
            _nodes.Remove(node);
            OnRemoveNode(node);
            IoCManager.Resolve<INodeGroupManager>().AddDirtyNodeGroup(this);
        }

        public void CombineGroup(INodeGroup newGroup)
        {
            if (newGroup.Nodes.Count < Nodes.Count)
            {
                newGroup.CombineGroup(this);
                return;
            }

            OnGivingNodesForCombine(newGroup);

            foreach (var node in Nodes)
            {
                node.NodeGroup = newGroup;
            }

            Removed = true;
        }

        /// <summary>
        ///     Causes all <see cref="Node"/>s to remake their groups. Called when a <see cref="Node"/> is removed
        ///     and may have split a group in two, so multiple new groups may need to be formed.
        /// </summary>
        public void RemakeGroup()
        {
            foreach (var node in Nodes)
            {
                node.ClearNodeGroup();
            }

            var newGroups = new HashSet<INodeGroup>();

            foreach (var node in Nodes)
            {
                if (node.TryAssignGroupIfNeeded())
                {
                    node.SpreadGroup();
                    newGroups.Add(node.NodeGroup);
                }
            }

            AfterRemake(newGroups);

            Removed = true;
        }

        protected virtual void OnAddNode(Node node) { }

        protected virtual void OnRemoveNode(Node node) { }

        protected virtual void OnGivingNodesForCombine(INodeGroup newGroup) { }

        protected virtual void AfterRemake(IEnumerable<INodeGroup> newGroups) { }

        protected class NullNodeGroup : INodeGroup
        {
            public IReadOnlyList<Node> Nodes => _nodes;
            private readonly List<Node> _nodes = new();
            public void Initialize(Node sourceNode) { }
            public void AddNode(Node node) { }
            public void CombineGroup(INodeGroup newGroup) { }
            public void RemoveNode(Node node) { }
            public void RemakeGroup() { }
        }
    }
}
