using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

[Serializable, NetSerializable]
public sealed class StopAttackEvent : EntityEventArgs
{
    public readonly EntityUid Weapon;

    public StopAttackEvent(EntityUid weapon)
    {
        Weapon = weapon;
    }
}
