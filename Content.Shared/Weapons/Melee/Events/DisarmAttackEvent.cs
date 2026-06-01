using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

[Serializable, NetSerializable]
public sealed class DisarmAttackEvent : AttackEvent
{
    public NetEntity? Target;

    public DisarmAttackEvent(NetEntity? target, NetCoordinates coordinates, Content.Shared._Offbrand.Input.OffbrandTargetZone targetZone) : base(coordinates, targetZone) // Offbrand
    {
        Target = target;
    }
}
