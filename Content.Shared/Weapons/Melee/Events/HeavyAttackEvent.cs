using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised on the client when it attempts a heavy attack.
/// </summary>
[Serializable, NetSerializable]
public sealed class HeavyAttackEvent : AttackEvent
{
    public readonly NetEntity Weapon;

    /// <summary>
    /// As what the client swung at will not match server we'll have them tell us what they hit so we can verify.
    /// </summary>
    public List<NetEntity> Entities;

    public HeavyAttackEvent(NetEntity weapon, List<NetEntity> entities, NetCoordinates coordinates, GameTick? tick) : base(coordinates, tick)
    {
        Weapon = weapon;
        Entities = entities;
    }
}
