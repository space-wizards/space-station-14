using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Organizes themselves into distinct <see cref="INodeGroup"/>s with other <see cref="INode"/>s
    ///     that they can "reach" and have the same <see cref="INode.NodeGroupID"/>.
    /// </summary>
    public interface INode
    {
        /// <summary>
        ///     An ID used as a criteria for combining into groups. Determines which <see cref="INodeGroup"/>
        ///     implementation is used as a group, detailed in <see cref="INodeGroupFactory"/>.
        /// </summary>
        NodeGroupID NodeGroupID { get; }

        /// <summary>
        ///     The <see cref="INodeGroup"/> this <see cref="Node"/> is currently in.
        /// </summary>
        public INodeGroup NodeGroup { get; set; }

        void OnContainerInitialize();

        void OnContainerRemove();

        bool TryAssignGroupIfNeeded();

        void SpreadGroup();
    }

    public abstract class Node : INode
    {
        // <inheritdoc cref="INode"/>
        [ViewVariables]
        public NodeGroupID NodeGroupID { get; }

        // <inheritdoc cref="INode"/>
        [ViewVariables]
        public INodeGroup NodeGroup { get => _nodeGroup; set => SetNodeGroup(value); }
        private INodeGroup _nodeGroup;

        /// <summary>
        ///     The <see cref="NodeContainerComponent"/> this <see cref="Node"/> is held in.
        /// </summary>
        [ViewVariables]
        public NodeContainerComponent Container { get; private set; }

#pragma warning disable 649
        [Dependency] private readonly INodeGroupFactory _nodeGroupFactory;
#pragma warning restore 649

        protected Node(NodeGroupID nodeGroupID, NodeContainerComponent container)
        {
            NodeGroupID = nodeGroupID;
            Container = container;
        }

        public void OnContainerInitialize()
        {
            TryAssignGroupIfNeeded();
            CombineGroupWithReachable();
        }

        public void OnContainerRemove()
        {
            NodeGroup.RemoveNode(this);
            _nodeGroup = null;
            Container = null;
        }

        public bool TryAssignGroupIfNeeded()
        {
            if (NodeGroup != null)
            {
                return false;
            }
            NodeGroup = GetReachableCompatibleGroups().FirstOrDefault() ?? MakeNewGroup();
            return true;
        }

        public void SpreadGroup()
        {
            Debug.Assert(NodeGroup != null);
            foreach (var node in GetReachableCompatibleNodes().Where(node => node.NodeGroup == null))
            {
                node.NodeGroup = NodeGroup;
                node.SpreadGroup();
            }
        }

        /// <summary>
        ///     Strategy for how to find other reachable <see cref="INode"/>s to group with.
        /// </summary>
        protected abstract IEnumerable<INode> GetReachableNodes();

        private IEnumerable<INode> GetReachableCompatibleNodes()
        {
            return GetReachableNodes().Where(node => node.NodeGroupID == NodeGroupID)
                .Where(node => node != this);
        }

        private IEnumerable<INodeGroup> GetReachableCompatibleGroups()
        {
            return GetReachableCompatibleNodes().Select(node => node.NodeGroup)
                .Where(group => group != NodeGroup);
        }

        private void CombineGroupWithReachable()
        {
            Debug.Assert(NodeGroup != null);
            foreach (var group in GetReachableCompatibleGroups())
            {
                group.CombineGroup(NodeGroup);
            }
        }

        private void SetNodeGroup(INodeGroup newGroup)
        {
            _nodeGroup = newGroup;
            NodeGroup?.AddNode(this);
        }

        private INodeGroup MakeNewGroup()
        {
            return _nodeGroupFactory.MakeNodeGroup(NodeGroupID);
        }
    }
}
