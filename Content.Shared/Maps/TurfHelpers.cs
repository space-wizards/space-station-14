using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Decals;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Shared.Maps
{
    public static class TurfHelpers
    {
        /// <summary>
        ///     Attempts to get the turf at map indices with grid id or null if no such turf is found.
        /// </summary>
        public static TileRef GetTileRef(this Vector2i vector2i, EntityUid gridId, IMapManager? mapManager = null)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(gridId, out var grid))
                return default;

            if (!grid.TryGetTileRef(vector2i, out var tile))
                return default;

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

            if (!mapManager.TryGetGrid(coordinates.GetGridUid(entityManager), out var grid))
                return null;


            if (!grid.TryGetTileRef(coordinates, out var tile))
                return null;

            return tile;
        }

        public static bool TryGetTileRef(this EntityCoordinates coordinates, [NotNullWhen(true)] out TileRef? turf, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            return (turf = coordinates.GetTileRef(entityManager, mapManager)) != null;
        }

        /// <summary>
        ///     Returns the content tile definition for a tile.
        /// </summary>
        public static ContentTileDefinition GetContentTileDefinition(this Tile tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            tileDefinitionManager ??= IoCManager.Resolve<ITileDefinitionManager>();
            return (ContentTileDefinition)tileDefinitionManager[tile.TypeId];
        }

        /// <summary>
        ///     Returns whether a tile is considered space.
        /// </summary>
        public static bool IsSpace(this Tile tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            return tile.GetContentTileDefinition(tileDefinitionManager).IsSpace;
        }

        /// <summary>
        ///     Returns the content tile definition for a tile ref.
        /// </summary>
        public static ContentTileDefinition GetContentTileDefinition(this TileRef tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            return tile.Tile.GetContentTileDefinition(tileDefinitionManager);
        }

        /// <summary>
        ///     Returns whether a tile ref is considered space.
        /// </summary>
        public static bool IsSpace(this TileRef tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            return tile.Tile.IsSpace(tileDefinitionManager);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<EntityUid> GetEntitiesInTile(this TileRef turf, LookupFlags flags = LookupFlags.Static, EntityLookupSystem? lookupSystem = null)
        {
            lookupSystem ??= EntitySystem.Get<EntityLookupSystem>();

            if (!GetWorldTileBox(turf, out var worldBox))
                return Enumerable.Empty<EntityUid>();

            return lookupSystem.GetEntitiesIntersecting(turf.GridUid, worldBox, flags);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        public static IEnumerable<EntityUid> GetEntitiesInTile(this EntityCoordinates coordinates, LookupFlags flags = LookupFlags.Static, EntityLookupSystem? lookupSystem = null)
        {
            var turf = coordinates.GetTileRef();

            if (turf == null)
                return Enumerable.Empty<EntityUid>();

            return GetEntitiesInTile(turf.Value, flags, lookupSystem);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        public static IEnumerable<EntityUid> GetEntitiesInTile(this Vector2i indices, EntityUid gridId, LookupFlags flags = LookupFlags.Static, EntityLookupSystem? lookupSystem = null)
        {
            return GetEntitiesInTile(indices.GetTileRef(gridId), flags, lookupSystem);
        }

        /// <summary>
        /// Checks if a turf has something dense on it.
        /// </summary>
        public static bool IsBlockedTurf(this TileRef turf, bool filterMobs, EntityLookupSystem? physics = null, IEntitySystemManager? entSysMan = null)
        {
            // TODO: Deprecate this with entitylookup.
            if (physics == null)
            {
                IoCManager.Resolve(ref entSysMan);
                physics = entSysMan.GetEntitySystem<EntityLookupSystem>();
            }

            if (!GetWorldTileBox(turf, out var worldBox))
                return false;

            var entManager = IoCManager.Resolve<IEntityManager>();
            var query = physics.GetEntitiesIntersecting(turf.GridUid, worldBox);

            foreach (var ent in query)
            {
                // Yes, this can fail. Welp!
                if (!entManager.TryGetComponent(ent, out PhysicsComponent? body))
                    continue;

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

            return turf.GridIndices.ToEntityCoordinates(turf.GridUid, mapManager);
        }

        /// <summary>
        /// Creates a box the size of a tile, at the same position in the world as the tile.
        /// </summary>
        [Obsolete]
        private static bool GetWorldTileBox(TileRef turf, out Box2Rotated res)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var map = IoCManager.Resolve<IMapManager>();

            if (map.TryGetGrid(turf.GridUid, out var tileGrid))
            {
                var gridRot = entManager.GetComponent<TransformComponent>(tileGrid.Owner).WorldRotation;

                // This is scaled to 90 % so it doesn't encompass walls on other tiles.
                var tileBox = Box2.UnitCentered.Scale(0.9f);
                tileBox = tileBox.Scale(tileGrid.TileSize);
                var worldPos = tileGrid.GridTileToWorldPos(turf.GridIndices);
                tileBox = tileBox.Translated(worldPos);
                // Now tileBox needs to be rotated to match grid rotation
                res = new Box2Rotated(tileBox, gridRot, worldPos);
                return true;
            }

            // Have to "return something"
            res = Box2Rotated.UnitCentered;
            return false;
        }
    }
}
