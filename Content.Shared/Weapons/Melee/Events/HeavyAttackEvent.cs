using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised on the client when it attempts a heavy attack.
/// </summary>
[Serializable, NetSerializable]
public sealed class HeavyAttackEvent : AttackEvent
{
    public readonly EntityUid Weapon;

    public HeavyAttackEvent(EntityUid weapon, EntityCoordinates coordinates) : base(coordinates)
    {
        Weapon = weapon;
    }
}
