using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared.Coordinates
{
    public static class EntityCoordinatesExtensions
    {
        public static EntityCoordinates ToCoordinates(this EntityUid id)
        {
            return new EntityCoordinates(id, new Vector2(0, 0));
        }

        public static EntityCoordinates ToCoordinates(this EntityUid id, Vector2 offset)
        {
            return new EntityCoordinates(id, offset);
        }

        public static EntityCoordinates ToCoordinates(this EntityUid id, float x, float y)
        {
            return new EntityCoordinates(id, x, y);
        }
    }
}
