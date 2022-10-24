using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised when a light attack is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class LightAttackEvent : AttackEvent
{
    public readonly EntityUid? Target;
    public readonly EntityUid Weapon;

    public LightAttackEvent(EntityUid? target, EntityUid weapon, EntityCoordinates coordinates) : base(coordinates)
    {
        Target = target;
        Weapon = weapon;
    }
}
