#nullable enable
using System.Collections.Generic;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.NodeContainer.Nodes
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

        [ViewVariables] public INodeGroup? NodeGroup;

        [ViewVariables] public IEntity Owner { get; private set; } = default!;

        /// <summary>
        ///     If this node should be considered for connection by other nodes.
        /// </summary>
        public bool Connectable => !Deleting && Anchored;

        protected bool Anchored => !NeedAnchored || Owner.Transform.Anchored;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("needAnchored")]
        private bool NeedAnchored { get; } = true;

        /// <summary>
        ///    Prevents a node from being used by other nodes while midway through removal.
        /// </summary>
        public bool Deleting;

        public readonly HashSet<Node> ReachableNodes = new();

        internal int FloodGen;
        internal int UndirectGen;
        internal bool FlaggedForFlood;
        internal int NetId;
        public string Name = default!;

        public virtual void Initialize(IEntity owner)
        {
            Owner = owner;
        }

        public virtual void OnContainerStartup()
        {
            EntitySystem.Get<NodeGroupSystem>().QueueReflood(this);
        }

        public void CreateSingleNetImmediate()
        {
            EntitySystem.Get<NodeGroupSystem>().CreateSingleNetImmediate(this);
        }

        public void AnchorUpdate()
        {
            if (Anchored)
            {
                EntitySystem.Get<NodeGroupSystem>().QueueReflood(this);
            }
            else
            {
                EntitySystem.Get<NodeGroupSystem>().QueueNodeRemove(this);
            }
        }

        public virtual void AnchorStateChanged()
        {
        }

        public virtual void OnPostRebuild()
        {

        }

        public virtual void OnContainerShutdown()
        {
            Deleting = true;
            EntitySystem.Get<NodeGroupSystem>().QueueNodeRemove(this);
        }

        /// <summary>
        ///     How this node will attempt to find other reachable <see cref="Node"/>s to group with.
        ///     Returns a set of <see cref="Node"/>s to consider grouping with. Should not return this current <see cref="Node"/>.
        /// </summary>
        public abstract IEnumerable<Node> GetReachableNodes();
    }
}
