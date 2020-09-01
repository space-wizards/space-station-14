#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Physics;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Maps
{
    public static class TurfHelpers
    {
        /// <summary>
        ///     Returns the content tile definition for a tile.
        /// </summary>
        public static ContentTileDefinition GetContentTileDefinition(this Tile tile)
        {
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            return (ContentTileDefinition)tileDefinitionManager[tile.TypeId];
        }

        /// <summary>
        ///     Attempts to get the turf at map indices with grid id or null if no such turf is found.
        /// </summary>
        public static TileRef? GetTileRef(this MapIndices mapIndices, GridId gridId)
        {
            if (!gridId.IsValid())
                return null;

            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(gridId, out var grid))
                return null;

            if (!grid.TryGetTileRef(mapIndices, out var tile))
                return null;

            return tile;
        }

        /// <summary>
        ///     Attempts to get the turf at a certain coordinates or null if no such turf is found.
        /// </summary>
        public static TileRef? GetTileRef(this GridCoordinates coordinates)
        {
            if (!coordinates.GridID.IsValid())
                return null;

            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(coordinates.GridID, out var grid))
                return null;

            if (!grid.TryGetTileRef(coordinates.ToMapIndices(mapManager), out var tile))
                return null;

            return tile;
        }

        public static bool TryGetTileRef(this GridCoordinates coordinates, [NotNullWhen(true)] out TileRef? turf)
        {
            return (turf = coordinates.GetTileRef()) != null;
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        public static IEnumerable<IEntity> GetEntitiesInTile(this TileRef turf, bool approximate = false)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            return entityManager.GetEntitiesIntersecting(turf.MapIndex, GetWorldTileBox(turf), approximate);
        }

        /// <summary>
        /// Checks if a turf has something dense on it.
        /// </summary>
        public static bool IsBlockedTurf(this TileRef turf, bool filterMobs)
        {
            var physics = IoCManager.Resolve<IPhysicsManager>();

            var worldBox = GetWorldTileBox(turf);

            var query = physics.GetCollidingEntities(turf.MapIndex, in worldBox);

            foreach (var body in query)
            {
                if (body.CanCollide && body.Hard && (body.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                    return true;

                if (filterMobs && (body.CollisionLayer & (int) CollisionGroup.MobMask) != 0)
                    return true;
            }

            return false;
        }

        public static GridCoordinates GridPosition(this TileRef turf)
        {
            return new GridCoordinates(turf.X, turf.Y, turf.GridIndex);
        }

        /// <summary>
        /// Creates a box the size of a tile, at the same position in the world as the tile.
        /// </summary>
        private static Box2 GetWorldTileBox(TileRef turf)
        {
            var map = IoCManager.Resolve<IMapManager>();
            var tileGrid = map.GetGrid(turf.GridIndex);
            var tileBox = Box2.UnitCentered.Scale(tileGrid.TileSize);
            return tileBox.Translated(tileGrid.GridTileToWorldPos(turf.GridIndices));
        }

        /// <summary>
        /// Creates a box the size of a tile.
        /// </summary>
        private static Box2 GetTileBox(this TileRef turf)
        {
            var map = IoCManager.Resolve<IMapManager>();
            var tileGrid = map.GetGrid(turf.GridIndex);
            return Box2.UnitCentered.Scale(tileGrid.TileSize);
        }
    }
}
