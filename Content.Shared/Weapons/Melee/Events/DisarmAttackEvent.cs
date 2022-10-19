using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

[Serializable, NetSerializable]
public sealed class DisarmAttackEvent : AttackEvent
{
    public EntityUid? Target;

    public DisarmAttackEvent(EntityUid? target, EntityCoordinates coordinates) : base(coordinates)
    {
        Target = target;
    }
}
