using Content.Shared.NodeContainer;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.NodeContainer.Nodes
{
    /// <summary>
    ///     A <see cref="Node"/> that can reach other <see cref="AdjacentNode"/>s that are directly adjacent to it.
    /// </summary>
    [DataDefinition]
    public sealed partial class AdjacentNode : Node
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

            foreach (var (_, node) in NodeHelpers.GetCardinalNeighborNodes(nodeQuery, xform, grid, gridIndex, mapSystem))
            {
                if (node != this)
                    yield return node;
            }
        }
    }
}
