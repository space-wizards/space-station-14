using System.Diagnostics.CodeAnalysis;
using Content.Shared.NodeContainer;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.NodeContainer.Nodes
{
    /// <summary>
    ///     Helper utilities for implementing <see cref="Node"/>.
    /// </summary>
    public static class NodeHelpers
    {
        public static IEnumerable<Node> GetNodesInTile(EntityQuery<NodeContainerComponent> nodeQuery, Entity<MapGridComponent> grid, Vector2i coords, SharedMapSystem mapSystem)
        {
            foreach (var entityUid in mapSystem.GetAnchoredEntities(grid, coords))
            {
                if (!nodeQuery.TryGetComponent(entityUid, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    yield return node;
                }
            }
        }

        [Obsolete("Use the overload that passes in Entity<MapGridComponent> and SharedMapSystem")]
        public static IEnumerable<Node> GetNodesInTile(EntityQuery<NodeContainerComponent> nodeQuery, MapGridComponent grid, Vector2i coords)
        {
            return GetNodesInTile(nodeQuery, (grid.Owner, grid), coords, IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>());
        }

        public static IEnumerable<(Direction dir, Node node)> GetCardinalNeighborNodes(
            EntityQuery<NodeContainerComponent> nodeQuery,
            Entity<MapGridComponent> grid,
            Vector2i coords,
            SharedMapSystem mapSystem,
            bool includeSameTile = true)
        {
            foreach (var (dir, entityUid) in GetCardinalNeighborCells(grid, coords, mapSystem, includeSameTile))
            {
                if (!nodeQuery.TryGetComponent(entityUid, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    yield return (dir, node);
                }
            }
        }

        [Obsolete("Use the overload that passes in Entity<MapGridComponent> and SharedMapSystem")]
        public static IEnumerable<(Direction dir, Node node)> GetCardinalNeighborNodes(
            EntityQuery<NodeContainerComponent> nodeQuery,
            MapGridComponent grid,
            Vector2i coords,
            bool includeSameTile = true)
        {
            return GetCardinalNeighborNodes(nodeQuery, (grid.Owner, grid), coords, IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>(), includeSameTile);
        }

        [SuppressMessage("ReSharper", "EnforceForeachStatementBraces")]
        public static IEnumerable<(Direction dir, EntityUid entity)> GetCardinalNeighborCells(
            Entity<MapGridComponent> grid,
            Vector2i coords,
            SharedMapSystem mapSystem,
            bool includeSameTile = true)
        {
            if (includeSameTile)
            {
                foreach (var uid in mapSystem.GetAnchoredEntities(grid, coords))
                    yield return (Direction.Invalid, uid);
            }

            foreach (var uid in mapSystem.GetAnchoredEntities(grid, coords + (0, 1)))
                yield return (Direction.North, uid);

            foreach (var uid in mapSystem.GetAnchoredEntities(grid, coords + (0, -1)))
                yield return (Direction.South, uid);

            foreach (var uid in mapSystem.GetAnchoredEntities(grid, coords + (1, 0)))
                yield return (Direction.East, uid);

            foreach (var uid in mapSystem.GetAnchoredEntities(grid, coords + (-1, 0)))
                yield return (Direction.West, uid);
        }

        [Obsolete("Use the overload that passes in Entity<MapGridComponent> and SharedMapSystem")]
        public static IEnumerable<(Direction dir, EntityUid entity)> GetCardinalNeighborCells(
            MapGridComponent grid,
            Vector2i coords,
            bool includeSameTile = true)
        {
            return GetCardinalNeighborCells((grid.Owner, grid), coords, IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>(), includeSameTile);
        }
    }
}
