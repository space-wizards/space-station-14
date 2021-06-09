using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Utility
{
    public static class SnapgridHelper
    {
        public static void SnapToGrid(this IEntity entity, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            entity.Transform.Coordinates = entity.Transform.Coordinates.SnapToGrid(entityManager, mapManager);
        }

        public static EntityCoordinates SnapToGrid(this EntityCoordinates coordinates, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();
            mapManager ??= IoCManager.Resolve<IMapManager>();

            var gridId = coordinates.GetGridId(entityManager);

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
