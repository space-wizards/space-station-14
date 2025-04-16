using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Coordinates.Helpers;

public static class SnapgridHelper
{
    [Obsolete("Use SharedTransformSystem.SnapToGrid")]
    public static EntityCoordinates SnapToGrid(this EntityCoordinates coordinates, IEntityManager? entMan = null, IMapManager? mapManager = null)
    {
        IoCManager.Resolve(ref entMan, ref mapManager);
        return entMan.System<SharedTransformSystem>().SnapToGrid(coordinates);
    }

    [PublicAPI]
    public static EntityCoordinates SnapToGrid(this EntityCoordinates coordinates, MapGridComponent grid)
    {
        var tileSize = grid.TileSize;

        var localPos = coordinates.Position;

        var x = (int)Math.Floor(localPos.X / tileSize) + tileSize / 2f;
        var y = (int)Math.Floor(localPos.Y / tileSize) + tileSize / 2f;

        return new EntityCoordinates(coordinates.EntityId, x, y);
    }
}
