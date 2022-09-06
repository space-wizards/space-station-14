using Robust.Shared.Map;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised on the client when it attempts a heavy attack.
/// </summary>
public sealed class HeavyAttackEvent : AttackEvent
{
    public readonly EntityUid Weapon;

    public HeavyAttackEvent(EntityUid weapon, EntityCoordinates coordinates) : base(coordinates)
    {
        Weapon = weapon;
    }
}
