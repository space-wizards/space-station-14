using Content.Shared.NodeContainer;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.NodeContainer.Nodes
{
    [DataDefinition]
    public sealed partial class PortPipeNode : PipeNode
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

            foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, xform, grid, gridIndex, mapSystem))
            {
                if (node is PortablePipeNode)
                    yield return node;
            }

            foreach (var node in base.GetReachableNodes(uid, nodeQuery, xformQuery, gridQuery, entMan, mapSystem))
            {
                yield return node;
            }
        }
    }
}
