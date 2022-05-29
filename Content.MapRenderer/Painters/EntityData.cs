using Robust.Client.GameObjects;

namespace Content.MapRenderer.Painters
{
    public sealed class EntityData
    {
        public EntityData(SpriteComponent sprite, float x, float y)
        {
            Sprite = sprite;
            X = x;
            Y = y;
        }

        public SpriteComponent Sprite { get; }

        public float X { get; }

        public float Y { get; }
    }
}
