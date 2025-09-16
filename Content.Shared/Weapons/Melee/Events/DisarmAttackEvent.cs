using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Melee.Events;

[Serializable, NetSerializable]
public sealed class DisarmAttackEvent : AttackEvent
{
    public NetEntity? Target;

    public DisarmAttackEvent(NetEntity? target, NetCoordinates coordinates, GameTick? tick) : base(coordinates, tick)
    {
        Target = target;
    }
}
