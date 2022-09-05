using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised when a light attack is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class LightAttackEvent : AttackEvent
{
    public EntityUid Target;
    public EntityUid Weapon;

    public LightAttackEvent(EntityUid target, EntityUid weapon)
    {
        Target = target;
        Weapon = weapon;
    }
}
