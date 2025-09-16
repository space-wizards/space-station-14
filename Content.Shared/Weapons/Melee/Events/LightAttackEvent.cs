using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised when a light attack is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class LightAttackEvent : AttackEvent
{
    public readonly NetEntity? Target;
    public readonly NetEntity Weapon;

    public LightAttackEvent(NetEntity? target, NetEntity weapon, NetCoordinates coordinates, GameTick? tick) : base(coordinates, tick)
    {
        Target = target;
        Weapon = weapon;
    }
}
