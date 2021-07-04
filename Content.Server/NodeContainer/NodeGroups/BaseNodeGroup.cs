#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Content.Server.NodeContainer.NodeGroups
{
    /// <summary>
    ///     Maintains a collection of <see cref="Node"/>s, and performs operations requiring a list of
    ///     all connected <see cref="Node"/>s.
    /// </summary>
    public interface INodeGroup
    {
        bool Remaking { get; }

        IReadOnlyList<Node> Nodes { get; }

        void Create(NodeGroupID groupId);

        void Initialize(Node sourceNode);

        void RemoveNode(Node node);

        void LoadNodes(List<Node> groupNodes);

        // In theory, the SS13 curse ensures this method will never be called.
        void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups);

        // TODO: Why is this method needed?
        void QueueRemake();
    }

    [NodeGroup(NodeGroupID.Default, NodeGroupID.WireNet)]
    public class BaseNodeGroup : INodeGroup
    {
        public bool Remaking { get; set; }

        IReadOnlyList<Node> INodeGroup.Nodes => Nodes;

        [ViewVariables] public readonly List<Node> Nodes = new();

        [ViewVariables] public int NodeCount => Nodes.Count;

        /// <summary>
        ///     Debug variable to indicate that this NodeGroup should not be being used by anything.
        /// </summary>
        [ViewVariables]
        public bool Removed { get; set; } = false;

        [ViewVariables]
        protected GridId GridId { get; private set; }

        [ViewVariables]
        public int NetId;

        [ViewVariables]
        public NodeGroupID GroupId { get; private set; }

        public void Create(NodeGroupID groupId)
        {
            GroupId = groupId;
        }

        public virtual void Initialize(Node sourceNode)
        {
            // TODO: Can we get rid of this GridId?
            GridId = sourceNode.Owner.Transform.GridID;
        }

        public virtual void RemoveNode(Node node)
        {
        }

        public virtual void LoadNodes(
            List<Node> groupNodes)
        {
            Nodes.AddRange(groupNodes);
        }

        public virtual void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups) { }

        public void QueueRemake()
        {
            EntitySystem.Get<NodeGroupSystem>().QueueRemakeGroup(this);
        }
    }
}
