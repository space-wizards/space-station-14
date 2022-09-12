using Robust.Shared.Map;

namespace Content.Shared.Weapons.Melee.Events;

public sealed class DisarmAttackEvent : AttackEvent
{
    public EntityUid? Target;

    public DisarmAttackEvent(EntityUid? target, EntityCoordinates coordinates) : base(coordinates)
    {
        Target = target;
    }
}
