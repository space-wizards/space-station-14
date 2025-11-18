using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.MapRenderer.Painters;

public readonly record struct EntityData(EntityUid Owner, SpriteComponent Sprite, float X, float Y)
{
    public readonly EntityUid Owner = Owner;

    public readonly SpriteComponent Sprite = Sprite;

    public readonly float X = X;

    public readonly float Y = Y;
}
