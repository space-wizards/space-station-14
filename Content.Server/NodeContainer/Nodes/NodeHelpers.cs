using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map.Components;

namespace Content.Server.NodeContainer.Nodes
{
    /// <summary>
    ///     Helper utilities for implementing <see cref="Node"/>.
    /// </summary>
    public static class NodeHelpers
    {
        public static IEnumerable<Node> GetNodesInTile(
            EntityQuery<NodeContainerComponent> nodeQuery,
            TransformComponent xform,
            MapGridComponent grid,
            Vector2i coords,
            SharedMapSystem mapSystem)
        {
            if (xform.GridUid == null)
                yield break;

            foreach (var entityUid in mapSystem.GetAnchoredEntities(xform.GridUid.Value, grid, coords))
            {
                if (!nodeQuery.TryGetComponent(entityUid, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    yield return node;
                }
            }
        }

        public static IEnumerable<(Direction dir, Node node)> GetCardinalNeighborNodes(
            EntityQuery<NodeContainerComponent> nodeQuery,
            TransformComponent xform,
            MapGridComponent grid,
            Vector2i coords,
            SharedMapSystem mapSystem,
            bool includeSameTile = true)
        {
            foreach (var (dir, entityUid) in GetCardinalNeighborCells(xform, grid, coords, mapSystem, includeSameTile))
            {
                if (!nodeQuery.TryGetComponent(entityUid, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    yield return (dir, node);
                }
            }
        }

        [SuppressMessage("ReSharper", "EnforceForeachStatementBraces")]
        public static IEnumerable<(Direction dir, EntityUid entity)> GetCardinalNeighborCells(
            TransformComponent xform,
            MapGridComponent grid,
            Vector2i coords,
            SharedMapSystem mapSystem,
            bool includeSameTile = true)
        {
            if (xform.GridUid == null)
                yield break;

            var gridUid = xform.GridUid.Value;

            if (includeSameTile)
            {
                foreach (var uid in mapSystem.GetAnchoredEntities(gridUid, grid, coords))
                    yield return (Direction.Invalid, uid);
            }

            foreach (var uid in mapSystem.GetAnchoredEntities(gridUid, grid, coords + (0, 1)))
                yield return (Direction.North, uid);

            foreach (var uid in mapSystem.GetAnchoredEntities(gridUid, grid, coords + (0, -1)))
                yield return (Direction.South, uid);

            foreach (var uid in mapSystem.GetAnchoredEntities(gridUid, grid, coords + (1, 0)))
                yield return (Direction.East, uid);

            foreach (var uid in mapSystem.GetAnchoredEntities(gridUid, grid, coords + (-1, 0)))
                yield return (Direction.West, uid);
        }
    }
}
