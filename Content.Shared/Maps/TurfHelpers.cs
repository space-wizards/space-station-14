#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Physics;
using Content.Shared.Utility;
using Robust.Shared.GameObjects.Systems;
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
        public static TileRef? GetTileRef(this EntityCoordinates coordinates, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            if (!coordinates.IsValid(entityManager))
                return null;

            mapManager ??= IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(coordinates.GetGridId(entityManager), out var grid))
                return null;

            if (!grid.TryGetTileRef(coordinates, out var tile))
                return null;

            return tile;
        }

        public static bool TryGetTileRef(this EntityCoordinates coordinates, [NotNullWhen(true)] out TileRef? turf)
        {
            return (turf = coordinates.GetTileRef()) != null;
        }

        public static bool PryTile(this EntityCoordinates coordinates, IEntityManager? entityManager = null,
            IMapManager? mapManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();
            mapManager ??= IoCManager.Resolve<IMapManager>();

            return coordinates.ToMapIndices(entityManager, mapManager).PryTile(coordinates.GetGridId(entityManager));
        }

        public static bool PryTile(this MapIndices indices, GridId gridId,
            IMapManager? mapManager = null, ITileDefinitionManager? tileDefinitionManager = null, IEntityManager? entityManager = null)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();
            var grid = mapManager.GetGrid(gridId);
            var tileRef = grid.GetTileRef(indices);
            return tileRef.PryTile(mapManager, tileDefinitionManager, entityManager);
        }

        public static bool PryTile(this TileRef tileRef,
            IMapManager? mapManager = null, ITileDefinitionManager? tileDefinitionManager = null, IEntityManager? entityManager = null)
        {
            var tile = tileRef.Tile;
            var indices = tileRef.GridIndices;

            // If the arguments are null, resolve the needed dependencies.
            mapManager ??= IoCManager.Resolve<IMapManager>();
            tileDefinitionManager ??= IoCManager.Resolve<ITileDefinitionManager>();
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            if (tile.IsEmpty) return false;

            var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.TypeId];

            if (!tileDef.CanCrowbar) return false;

            var mapGrid = mapManager.GetGrid(tileRef.GridIndex);

            var plating = tileDefinitionManager[tileDef.BaseTurfs[^1]];

             mapGrid.SetTile(tileRef.GridIndices, new Tile(plating.TileId));

             var half = mapGrid.TileSize / 2f;

            //Actually spawn the relevant tile item at the right position and give it some random offset.
            var tileItem = entityManager.SpawnEntity(tileDef.ItemDropPrototypeName, indices.ToEntityCoordinates(mapManager, tileRef.GridIndex).Offset(new Vector2(half, half)));
            tileItem.RandomOffset(0.25f);
            return true;
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IEntity> GetEntitiesInTile(this TileRef turf, bool approximate = false)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            return entityManager.GetEntitiesIntersecting(turf.MapIndex, GetWorldTileBox(turf), approximate);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        public static IEnumerable<IEntity> GetEntitiesInTile(this EntityCoordinates coordinates, bool approximate = false)
        {
            var turf = coordinates.GetTileRef();

            if (turf == null)
                return Enumerable.Empty<IEntity>();

            return GetEntitiesInTile(turf.Value);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        public static IEnumerable<IEntity> GetEntitiesInTile(this MapIndices indices, GridId gridId, bool approximate = false)
        {
            var turf = indices.GetTileRef(gridId);

            if (turf == null)
                return Enumerable.Empty<IEntity>();

            return GetEntitiesInTile(turf.Value);
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

        public static EntityCoordinates GridPosition(this TileRef turf, IMapManager? mapManager = null)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();

            return turf.GridIndices.ToEntityCoordinates(mapManager, turf.GridIndex);
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
