#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Organizes themselves into distinct <see cref="INodeGroup"/>s with other <see cref="Node"/>s
    ///     that they can "reach" and have the same <see cref="Node.NodeGroupID"/>.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class Node
    {
        /// <summary>
        ///     An ID used as a criteria for combining into groups. Determines which <see cref="INodeGroup"/>
        ///     implementation is used as a group, detailed in <see cref="INodeGroupFactory"/>.
        /// </summary>
        [ViewVariables]
        [DataField("nodeGroupID")]
        public NodeGroupID NodeGroupID { get; private set; } = NodeGroupID.Default;

        [ViewVariables]
        public INodeGroup NodeGroup { get => _nodeGroup; set => SetNodeGroup(value); }
        private INodeGroup _nodeGroup = BaseNodeGroup.NullGroup;

        [ViewVariables]
        public IEntity Owner { get; private set; } = default!;

        [ViewVariables]
        private bool _needsGroup = true;

        /// <summary>
        ///     If this node should be considered for connection by other nodes.
        /// </summary>
        public bool Connectable => !_deleting && Anchored;

        private bool Anchored => !Owner.TryGetComponent<IPhysicsComponent>(out var physics) || physics.Anchored;

        /// <summary>
        ///    Prevents a node from being used by other nodes while midway through removal.
        /// </summary>
        private bool _deleting;

        public virtual void Initialize(IEntity owner)
        {
            Owner = owner;
        }

        public virtual void OnContainerStartup()
        {
            TryAssignGroupIfNeeded();
            CombineGroupWithReachable();
        }

        public void AnchorUpdate()
        {
            if (Anchored)
            {
                TryAssignGroupIfNeeded();
                CombineGroupWithReachable();
            }
            else
            {
                RemoveSelfFromGroup();
            }
        }

        public virtual void OnContainerShutdown()
        {
            _deleting = true;
            NodeGroup.RemoveNode(this);
        }

        public bool TryAssignGroupIfNeeded()
        {
            if (!_needsGroup || !Connectable)
            {
                return false;
            }
            NodeGroup = GetReachableCompatibleGroups().FirstOrDefault() ?? MakeNewGroup();
            return true;
        }

        public void SpreadGroup()
        {
            Debug.Assert(!_needsGroup);
            foreach (var node in GetReachableCompatibleNodes())
            {
                if (node._needsGroup)
                {
                    node.NodeGroup = NodeGroup;
                    node.SpreadGroup();
                }
            }
        }

        public void ClearNodeGroup()
        {
            _nodeGroup = BaseNodeGroup.NullGroup;
            _needsGroup = true;
        }

        protected void RefreshNodeGroup()
        {
            RemoveSelfFromGroup();
            TryAssignGroupIfNeeded();
            CombineGroupWithReachable();
        }

        /// <summary>
        ///     How this node will attempt to find other reachable <see cref="Node"/>s to group with.
        ///     Returns a set of <see cref="Node"/>s to consider grouping with. Should not return this current <see cref="Node"/>.
        /// </summary>
        protected abstract IEnumerable<Node> GetReachableNodes();

        private IEnumerable<Node> GetReachableCompatibleNodes()
        {
            foreach (var node in GetReachableNodes())
            {
                if (node.NodeGroupID == NodeGroupID && node.Connectable)
                {
                    yield return node;
                }
            }
        }

        private IEnumerable<INodeGroup> GetReachableCompatibleGroups()
        {
            foreach (var node in GetReachableCompatibleNodes())
            {
                if (!node._needsGroup)
                {
                    var group = node.NodeGroup;
                    if (group != NodeGroup)
                    {
                        yield return group;
                    }
                }
            }
        }

        private void CombineGroupWithReachable()
        {
            if (_needsGroup || !Connectable)
                return;

            foreach (var group in GetReachableCompatibleGroups())
            {
                NodeGroup.CombineGroup(group);
            }
        }

        private void SetNodeGroup(INodeGroup newGroup)
        {
            _nodeGroup = newGroup;
            NodeGroup.AddNode(this);
            _needsGroup = false;
        }

        private INodeGroup MakeNewGroup()
        {
            return IoCManager.Resolve<INodeGroupFactory>().MakeNodeGroup(this);
        }

        private void RemoveSelfFromGroup()
        {
            NodeGroup.RemoveNode(this);
            ClearNodeGroup();
        }
    }
}
