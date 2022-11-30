using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Map.Components;

namespace Content.Server.Electrocution
{
    [DataDefinition]
    public sealed class ElectrocutionNode : Node
    {
        [DataField("cable")]
        public EntityUid CableEntity;
        [DataField("node")]
        public string? NodeName;

        public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            MapGridComponent? grid,
            IEntityManager entMan)
        {
            if (!nodeQuery.TryGetComponent(CableEntity, out var nodeContainer))
                yield break;

            if (nodeContainer.TryGetNode(NodeName, out Node? node))
                yield return node;
        }
    }
}
