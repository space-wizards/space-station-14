using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised by the client if it pre-emptively stops a heavy attack.
/// </summary>
[Serializable, NetSerializable]
public sealed class StopHeavyAttackEvent : EntityEventArgs
{
    public readonly EntityUid Weapon;

    public StopHeavyAttackEvent(EntityUid weapon)
    {
        Weapon = weapon;
    }
}
