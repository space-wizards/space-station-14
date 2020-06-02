using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Organizes themselves into distinct <see cref="INodeGroup"/>s with other <see cref="Node"/>s
    ///     that they can "reach" and have the same <see cref="Node.NodeGroupID"/>.
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        ///     An ID used as a criteria for combining into groups. Determines which <see cref="INodeGroup"/>
        ///     implementation is used as a group, detailed in <see cref="NodeGroupFactory"/>.
        /// </summary>
        [ViewVariables]
        public NodeGroupID NodeGroupID { get; private set; }

        [ViewVariables]
        public INodeGroup NodeGroup { get => _nodeGroup; set => SetNodeGroup(value); }
        private INodeGroup _nodeGroup = BaseNodeGroup.Null;

        [ViewVariables]
        public IEntity Owner { get; private set; }

        public bool NeedsGroup { get; private set; } = true;

#pragma warning disable 649
        [Dependency] private readonly NodeGroupFactory _nodeGroupFactory;
#pragma warning restore 649

        public void Initialize(NodeGroupID nodeGroupID, IEntity owner)
        {
            NodeGroupID = nodeGroupID;
            Owner = owner;
        }

        public void OnContainerInitialize()
        {
            TryAssignGroupIfNeeded();
            CombineGroupWithReachable();
        }

        public void OnContainerRemove()
        {
            NodeGroup.RemoveNode(this);
        }

        public bool TryAssignGroupIfNeeded()
        {
            if (!NeedsGroup)
            {
                return false;
            }
            NodeGroup = GetReachableCompatibleGroups().FirstOrDefault() ?? MakeNewGroup();
            return true;
        }

        public void StartSpreadingGroup()
        {
            NodeGroup.BeforeRemakeSpread(); 
            SpreadGroup();
            NodeGroup.AfterRemakeSpread();
        }

        public void SpreadGroup()
        {
            Debug.Assert(!NeedsGroup);
            foreach (var node in GetReachableCompatibleNodes().Where(node => node.NeedsGroup == true))
            {
                node.NodeGroup = NodeGroup;
                node.SpreadGroup();
            }
        }

        public void ClearNodeGroup()
        {
            _nodeGroup = BaseNodeGroup.Null;
            NeedsGroup = true;
        }

        /// <summary>
        ///     How this node will attempt to find other reachable <see cref="Node"/>s to group with.
        ///     Returns a set of <see cref="Node"/>s to consider grouping with. Should not return this current <see cref="Node"/>. 
        /// </summary>
        protected abstract IEnumerable<Node> GetReachableNodes();

        private IEnumerable<Node> GetReachableCompatibleNodes()
        {
            return GetReachableNodes().Where(node => node.NodeGroupID == NodeGroupID);
        }

        private IEnumerable<INodeGroup> GetReachableCompatibleGroups()
        {
            return GetReachableCompatibleNodes().Where(node => node.NeedsGroup == false)
                .Select(node => node.NodeGroup)
                .Where(group => group != NodeGroup);
        }

        private void CombineGroupWithReachable()
        {
            Debug.Assert(!NeedsGroup);
            foreach (var group in GetReachableCompatibleGroups())
            {
                NodeGroup.CombineGroup(group);
            }
        }

        private void SetNodeGroup(INodeGroup newGroup)
        {
            _nodeGroup = newGroup;
            NodeGroup.AddNode(this);
            NeedsGroup = false;
        }

        private INodeGroup MakeNewGroup()
        {
            return _nodeGroupFactory.MakeNodeGroup(NodeGroupID);
        }
    }
}
