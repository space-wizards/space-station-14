using Robust.Shared.Map;

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

        public static EntityCoordinates ToCoordinates(this MapGridComponent grid, Vector2 offset)
        {
            return ToCoordinates(grid.Owner, offset);
        }

        public static EntityCoordinates ToCoordinates(this MapGridComponent grid, float x, float y)
        {
            return ToCoordinates(grid.Owner, x, y);
        }

        public static EntityCoordinates ToCoordinates(this MapGridComponent grid)
        {
            return ToCoordinates(grid.Owner, Vector2.Zero);
        }
    }
}
