using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes
{
    [DataDefinition]
    public sealed partial class CableTerminalPortNode : Node
    {
        public override IEnumerable<Node> GetReachableNodes(
            EntityUid uid,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<MapGridComponent> gridQuery,
            IEntityManager entMan,
            SharedMapSystem mapSystem)
        {
            if (!xformQuery.TryGetComponent(uid, out var xform)
                || !xform.Anchored
                || xform.GridUid == null)
                yield break;

            if (!gridQuery.TryGetComponent(xform.GridUid.Value, out var grid))
                yield break;

            var gridIndex = mapSystem.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);

            var nodes = NodeHelpers.GetCardinalNeighborNodes(nodeQuery, xform, grid, gridIndex, mapSystem, includeSameTile: false);
            foreach (var (dir, node) in nodes)
            {
                if (node is CableTerminalNode
                    && dir != Direction.Invalid
                    && xformQuery.GetComponent(node.Owner).LocalRotation.GetCardinalDir().GetOpposite() == dir)
                    yield return node;
            }
        }
    }
}
