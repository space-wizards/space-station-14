using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.NodeContainer.Nodes
{
    /// <summary>
    ///     Helper utilities for implementing <see cref="Node"/>.
    /// </summary>
    public static class NodeHelpers
    {
        public static IEnumerable<Node> GetNodesInTile(IEntityManager entMan, IMapGrid grid, Vector2i coords)
        {
            foreach (var entityUid in grid.GetAnchoredEntities(coords))
            {
                if (!entMan.TryGetComponent(entityUid, out NodeContainerComponent? container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    yield return node;
                }
            }
        }

        public static IEnumerable<(Direction dir, Node node)> GetCardinalNeighborNodes(
            IEntityManager entMan,
            IMapGrid grid,
            Vector2i coords,
            bool includeSameTile = true)
        {
            foreach (var (dir, entityUid) in GetCardinalNeighborCells(grid, coords, includeSameTile))
            {
                if (!entMan.TryGetComponent(entityUid, out NodeContainerComponent? container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    yield return (dir, node);
                }
            }
        }

        [SuppressMessage("ReSharper", "EnforceForeachStatementBraces")]
        public static IEnumerable<(Direction dir, EntityUid entity)> GetCardinalNeighborCells(
            IMapGrid grid,
            Vector2i coords,
            bool includeSameTile = true)
        {
            if (includeSameTile)
            {
                foreach (var uid in grid.GetAnchoredEntities(coords))
                    yield return (Direction.Invalid, uid);
            }

            foreach (var uid in grid.GetAnchoredEntities(coords + (0, 1)))
                yield return (Direction.North, uid);

            foreach (var uid in grid.GetAnchoredEntities(coords + (0, -1)))
                yield return (Direction.South, uid);

            foreach (var uid in grid.GetAnchoredEntities(coords + (1, 0)))
                yield return (Direction.East, uid);

            foreach (var uid in grid.GetAnchoredEntities(coords + (-1, 0)))
                yield return (Direction.West, uid);
        }

        public static Vector2i TileOffsetForDir(Direction dir)
        {
            return dir switch
            {
                Direction.Invalid => (0, 0),
                Direction.South => (0, -1),
                Direction.SouthEast => (1, -1),
                Direction.East => (1, 0),
                Direction.NorthEast => (1, 1),
                Direction.North => (0, 1),
                Direction.NorthWest => (-1, 1),
                Direction.West => (-1, 0),
                Direction.SouthWest => (-1, -1),
                _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
            };
        }
    }
}
