using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeGroup
{
    /// <summary>
    ///     Organizes themselves into distinct <see cref="INodeGroup"/>s with other <see cref="@string"/>s
    ///     that they can "reach" and have the same <see cref="NodeGroupID"/>.
    /// </summary>
    public interface INode
    {
        NodeGroupID NodeGroupID { get; }

        public INodeGroup NodeGroup { get; set; }

        bool TryAssignGroupIfNeeded();

        void SpreadRemadeGroup();
    }

    public abstract class NodeComponent : Component, INode
    {
        [ViewVariables]
        public NodeGroupID NodeGroupID => _nodeGroupID;
        private NodeGroupID _nodeGroupID;

        [ViewVariables]
        public INodeGroup NodeGroup { get => _nodeGroup; set => SetNodeGroup(value); }
        private INodeGroup _nodeGroup;

#pragma warning disable 649
        [Dependency] private readonly INodeGroupFactory _nodeGroupFactory;
#pragma warning restore 649

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _nodeGroupID, "nodeGroupID", NodeGroupID.Default);
        }

        public override void Initialize()
        {
            base.Initialize();
            TryAssignGroupIfNeeded();
            SpreadGroup();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            NodeGroup.RemoveNode(this);
            _nodeGroup = null;
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

        public void SpreadRemadeGroup()
        {
            Debug.Assert(NodeGroup != null);
            foreach (var node in GetReachableCompatibleNodes().Where(node => node.NodeGroup == null))
            {
                node.NodeGroup = NodeGroup;
                node.SpreadRemadeGroup();
            }
        }

        /// <summary>
        ///     Strategy for how to find other reachable <see cref="@string"/>s to group with.
        /// </summary>
        protected abstract IEnumerable<NodeComponent> GetReachableNodes();

        private IEnumerable<NodeComponent> GetReachableCompatibleNodes()
        {
            return GetReachableNodes().Where(node => node.NodeGroupID == NodeGroupID)
                .Where(node => node != this);
        }

        private IEnumerable<INodeGroup> GetReachableCompatibleGroups()
        {
            return GetReachableCompatibleNodes().Select(node => node.NodeGroup)
                .Where(group => group != NodeGroup);
        }

        private void SpreadGroup(bool remakingGroup = false)
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

    public enum NodeGroupID
    {
        Default,
        HVPower,
        MVPower,
        LVPower,
    }
}
