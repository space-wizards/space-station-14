using Content.Shared.Physics;
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
