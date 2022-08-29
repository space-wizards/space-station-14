using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee;

[Serializable, NetSerializable]
public sealed class MeleeEffectEvent : EntityEventArgs
{
    public List<EntityUid> HitEntities;

    public MeleeEffectEvent(List<EntityUid> hitEntities)
    {
        HitEntities = hitEntities;
    }
}
