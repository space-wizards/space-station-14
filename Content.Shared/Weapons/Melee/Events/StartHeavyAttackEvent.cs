using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

[Serializable, NetSerializable]
public sealed class StartHeavyAttackEvent : EntityEventArgs
{
    public readonly EntityUid Weapon;

    public StartHeavyAttackEvent(EntityUid weapon)
    {
        Weapon = weapon;
    }
}
