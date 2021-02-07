using System;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Atmos
{
    public static class EntityNetworkUtils
    {
        public static Vector2i CardinalToIntVec(this Direction dir)
        {
            switch (dir)
            {
                case Direction.North:
                    return new Vector2i(0, 1);
                case Direction.East:
                    return new Vector2i(1, 0);
                case Direction.South:
                    return new Vector2i(0, -1);
                case Direction.West:
                    return new Vector2i(-1, 0);
                default:
                    throw new ArgumentException($"Direction dir {dir} is not a cardinal direction", nameof(dir));
            }
        }

        public static Vector2i Offset(this Vector2i pos, Direction dir)
        {
            return pos + (Vector2i) dir.CardinalToIntVec();
        }
    }
}
