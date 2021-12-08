using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Coordinates.Helpers
{
    public static class SnapgridHelper
    {
        public static void SnapToGrid(this EntityUid entity, IEntityManager? entMan = null, IMapManager? mapManager = null)
        {
            IoCManager.Resolve(ref entMan, ref mapManager);
            var transform = entMan.GetComponent<TransformComponent>(entity);
            transform.Coordinates = transform.Coordinates.SnapToGrid(entMan, mapManager);
        }

        public static EntityCoordinates SnapToGrid(this EntityCoordinates coordinates, IEntityManager? entMan = null, IMapManager? mapManager = null)
        {
            IoCManager.Resolve(ref entMan, ref mapManager);

            var gridId = coordinates.GetGridId(entMan);

            var tileSize = 1f;

            if (gridId.IsValid())
            {
                var grid = mapManager.GetGrid(gridId);
                tileSize = grid.TileSize;
            }

            var localPos = coordinates.Position;

            var x = (int)Math.Floor(localPos.X / tileSize) + tileSize / 2f;
            var y = (int)Math.Floor(localPos.Y / tileSize) + tileSize / 2f;

            return new EntityCoordinates(coordinates.EntityId, x, y);
        }

        public static EntityCoordinates SnapToGrid(this EntityCoordinates coordinates, IMapGrid grid)
        {
            var tileSize = grid.TileSize;

            var localPos = coordinates.Position;

            var x = (int)Math.Floor(localPos.X / tileSize) + tileSize / 2f;
            var y = (int)Math.Floor(localPos.Y / tileSize) + tileSize / 2f;

            return new EntityCoordinates(coordinates.EntityId, x, y);
        }
    }
}
