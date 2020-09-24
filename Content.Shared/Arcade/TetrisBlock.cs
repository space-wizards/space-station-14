using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade
{
    [Serializable, NetSerializable]
    public struct TetrisBlock
    {
        public readonly Vector2i Position;
        public readonly TetrisBlockColor Color;

        public TetrisBlock(Vector2i position, TetrisBlockColor color)
        {
            Position = position;
            Color = color;
        }

        [Serializable, NetSerializable]
        public enum TetrisBlockColor
        {
            Red,
            Orange,
            Yellow,
            Green,
            Blue,
            LightBlue,
            Purple
        }
    }

    public static class TetrisVector2Extensions{
        public static TetrisBlock ToTetrisBlock(this Vector2i vector2, TetrisBlock.TetrisBlockColor color)
        {
            return new TetrisBlock(vector2, color);
        }

        public static Vector2i AddToX(this Vector2i vector2, int amount)
        {
            return new Vector2i(vector2.X + amount, vector2.Y);
        }
        public static Vector2i AddToY(this Vector2i vector2, int amount)
        {
            return new Vector2i(vector2.X, vector2.Y + amount);
        }

        public static Vector2i Rotate90DegreesAsOffset(this Vector2i vector)
        {
            return new Vector2i(-vector.Y, vector.X);
        }

    }
}
