using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    [NodeState(NodeStateID.Default)]
    public class BaseNodeState
    {
        public Node Node { get; private set; }

        public void Initialize(Node node)
        {
            Node = node;
        }

        public virtual void ExposeData(ObjectSerializer serializer)
        {
        }

        /// <summary>
        ///     How this node will attempt to find other reachable <see cref="Node"/>s to group with.
        ///     Returns a set of <see cref="Node"/>s to consider grouping with. Should not return this current <see cref="Node"/>.
        /// </summary>
        public virtual IEnumerable<Node> GetReachableNodes()
        {
            yield break;
        }
    }
}
