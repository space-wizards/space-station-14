using Robust.Shared.Map.Components;

namespace Content.Server.NodeContainer.Nodes
{
    /// <summary>
    ///     A <see cref="Node"/> that can reach other <see cref="AdjacentNode"/>s that are directly adjacent to it.
    /// </summary>
    [DataDefinition]
    public sealed partial class AdjacentNode : Node
    {
        public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            MapGridComponent? grid,
            IEntityManager entMan)
        {
            if (!xform.Anchored || grid == null)
                yield break;

            var gridIndex = grid.TileIndicesFor(xform.Coordinates);

            foreach (var (_, node) in NodeHelpers.GetCardinalNeighborNodes(nodeQuery, grid, gridIndex))
            {
                if (node != this)
                    yield return node;
            }
        }
    }
}
