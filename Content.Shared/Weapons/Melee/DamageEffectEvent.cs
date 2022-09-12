using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// Raised on the server and sent to a client to play the damage animation.
/// </summary>
[Serializable, NetSerializable]
public sealed class DamageEffectEvent : EntityEventArgs
{
    public List<EntityUid> Entities;

    public DamageEffectEvent(List<EntityUid> entities)
    {
        Entities = entities;
    }
}
