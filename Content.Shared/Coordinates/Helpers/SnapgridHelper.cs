using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Coordinates.Helpers
{
    public static class SnapgridHelper
    {
        public static EntityCoordinates SnapToGrid(this EntityCoordinates coordinates, IEntityManager? entMan = null, IMapManager? mapManager = null)
        {
            IoCManager.Resolve(ref entMan, ref mapManager);

            var gridId = coordinates.GetGridUid(entMan);

            if (gridId == null)
            {
                var xformSys = entMan.System<SharedTransformSystem>();
                var mapPos = coordinates.ToMap(entMan, xformSys);
                var mapX = (int)Math.Floor(mapPos.X) + 0.5f;
                var mapY = (int)Math.Floor(mapPos.Y) + 0.5f;
                mapPos = new MapCoordinates(new Vector2(mapX, mapY), mapPos.MapId);
                return EntityCoordinates.FromMap(coordinates.EntityId, mapPos, xformSys);
            }

            var grid = entMan.GetComponent<MapGridComponent>(gridId.Value);
            var tileSize = grid.TileSize;
            var localPos = coordinates.WithEntityId(gridId.Value).Position;
            var x = (int)Math.Floor(localPos.X / tileSize) + tileSize / 2f;
            var y = (int)Math.Floor(localPos.Y / tileSize) + tileSize / 2f;
            var gridPos = new EntityCoordinates(gridId.Value, new Vector2(x, y));
            return gridPos.WithEntityId(coordinates.EntityId);
        }

        public static EntityCoordinates SnapToGrid(this EntityCoordinates coordinates, MapGridComponent grid)
        {
            var tileSize = grid.TileSize;

            var localPos = coordinates.Position;

            var x = (int)Math.Floor(localPos.X / tileSize) + tileSize / 2f;
            var y = (int)Math.Floor(localPos.Y / tileSize) + tileSize / 2f;

            return new EntityCoordinates(coordinates.EntityId, x, y);
        }
    }
}
