using System.Collections.Generic;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Electrocution
{
    [DataDefinition]
    public sealed class ElectrocutionNode : Node
    {
        [DataField("cable")]
        public EntityUid CableEntity;
        [DataField("node")]
        public string NodeName = default!;

        public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            IMapGrid? grid,
            IEntityManager entMan)
        {
            if (!nodeQuery.TryGetComponent(CableEntity, out var nodeContainer))
                yield break;

            if (nodeContainer.TryGetNode(NodeName, out Node? node))
                yield return node;
        }
    }
}
