using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Coordinates
{
    public static class EntityCoordinatesExtensions
    {
        public static EntityCoordinates ToCoordinates(this EntityUid id, Vector2 offset)
        {
            return new(id, offset);
        }

        public static EntityCoordinates ToCoordinates(this EntityUid id, float x, float y)
        {
            return new(id, x, y);
        }

        public static EntityCoordinates ToCoordinates(this IMapGrid grid, Vector2 offset)
        {
            return ToCoordinates(grid.GridEntityId, offset);
        }

        public static EntityCoordinates ToCoordinates(this IMapGrid grid, float x, float y)
        {
            return ToCoordinates(grid.GridEntityId, x, y);
        }

        public static EntityCoordinates ToCoordinates(this IMapGrid grid)
        {
            return ToCoordinates(grid.GridEntityId, Vector2.Zero);
        }
    }
}
