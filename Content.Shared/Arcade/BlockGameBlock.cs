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
            Purple,
            GhostRed,
            GhostOrange,
            GhostYellow,
            GhostGreen,
            GhostBlue,
            GhostLightBlue,
            GhostPurple,
        }

        public static BlockGameBlockColor ToGhostBlockColor(BlockGameBlockColor inColor)
        {
            return inColor switch
            {
                BlockGameBlockColor.Red => BlockGameBlockColor.GhostRed,
                BlockGameBlockColor.Orange => BlockGameBlockColor.GhostOrange,
                BlockGameBlockColor.Yellow => BlockGameBlockColor.GhostYellow,
                BlockGameBlockColor.Green => BlockGameBlockColor.GhostGreen,
                BlockGameBlockColor.Blue => BlockGameBlockColor.GhostBlue,
                BlockGameBlockColor.LightBlue => BlockGameBlockColor.GhostLightBlue,
                BlockGameBlockColor.Purple => BlockGameBlockColor.GhostPurple,
                _ => inColor
            };
        }

        public static Color ToColor(BlockGameBlockColor inColor)
        {
            return inColor switch
            {
                BlockGameBlockColor.Red => Color.Red,
                BlockGameBlockColor.Orange => Color.Orange,
                BlockGameBlockColor.Yellow => Color.Yellow,
                BlockGameBlockColor.Green => Color.Lime,
                BlockGameBlockColor.Blue => Color.Blue,
                BlockGameBlockColor.Purple => Color.DarkOrchid,
                BlockGameBlockColor.LightBlue => Color.Cyan,
                BlockGameBlockColor.GhostRed => Color.Red.WithAlpha(0.33f),
                BlockGameBlockColor.GhostOrange => Color.Orange.WithAlpha(0.33f),
                BlockGameBlockColor.GhostYellow => Color.Yellow.WithAlpha(0.33f),
                BlockGameBlockColor.GhostGreen => Color.Lime.WithAlpha(0.33f),
                BlockGameBlockColor.GhostBlue => Color.Blue.WithAlpha(0.33f),
                BlockGameBlockColor.GhostPurple => Color.DarkOrchid.WithAlpha(0.33f),
                BlockGameBlockColor.GhostLightBlue => Color.Cyan.WithAlpha(0.33f),
                _ => Color.Olive //olive is error
            };
        }
    }

    public static class BlockGameVector2Extensions
    {
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
