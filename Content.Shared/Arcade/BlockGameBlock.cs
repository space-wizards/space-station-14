using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade
{
    [Serializable, NetSerializable]
    public struct BlockGameBlock
    {
        public Vector2i Position;
        public readonly BlockGameBlockColor GameBlockColor;

        public BlockGameBlock(Vector2i position, BlockGameBlockColor gameBlockColor)
        {
            Position = position;
            GameBlockColor = gameBlockColor;
        }

        [Serializable, NetSerializable]
        public enum BlockGameBlockColor
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

    public static class BlockGameVector2Extensions{
        public static BlockGameBlock ToBlockGameBlock(this Vector2i vector2, BlockGameBlock.BlockGameBlockColor gameBlockColor)
        {
            return new(vector2, gameBlockColor);
        }

        public static Vector2i AddToX(this Vector2i vector2, int amount)
        {
            return new(vector2.X + amount, vector2.Y);
        }
        public static Vector2i AddToY(this Vector2i vector2, int amount)
        {
            return new(vector2.X, vector2.Y + amount);
        }

        public static Vector2i Rotate90DegreesAsOffset(this Vector2i vector)
        {
            return new(-vector.Y, vector.X);
        }

    }
}
