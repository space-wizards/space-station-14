using Robust.Client.GameObjects;

namespace Content.MapRenderer.Painters
{
    public class EntityData
    {
        public EntityData(SpriteComponent sprite, int x, int y)
        {
            Sprite = sprite;
            X = x;
            Y = y;
        }

        public SpriteComponent Sprite { get; }

        public int X { get; }

        public int Y { get; }
    }
}
