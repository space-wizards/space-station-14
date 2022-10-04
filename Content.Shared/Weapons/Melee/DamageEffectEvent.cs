using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// Raised on the server and sent to a client to play the damage animation.
/// </summary>
[Serializable, NetSerializable]
public sealed class DamageEffectEvent : EntityEventArgs
{
    /// <summary>
    /// Color to play for the damage flash.
    /// </summary>
    public Color Color;

    public List<EntityUid> Entities;

    public DamageEffectEvent(Color color, List<EntityUid> entities)
    {
        Color = color;
        Entities = entities;
    }
}
