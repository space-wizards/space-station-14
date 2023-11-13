using Robust.Shared.Serialization;

namespace Content.Shared.Sprite;

/// <summary>
/// Sent from server to client to copy the sprite component from a prototype to an entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class CopySpriteEvent
{
    /// <summary>
    /// Entity prototype to copy the sprite from
    /// </summary>
    public string Prototype;

    /// <summary>
    /// Entity to copy the sprite component to.
    /// </summary>
    public NetEntity Entity;

    public CopySpriteEvent(string prototype, NetEntity entity)
    {
        Prototype = prototype;
        Entity = entity;
    }
}
