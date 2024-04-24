using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity whenever it has a movement input change.
/// </summary>
[ByRefEvent]
public readonly struct MoveInputEvent
{
    public readonly EntityUid Entity;
    public readonly InputMoverComponent Component;
    public readonly MoveButtons OldMovement;

    public bool HasDirectionalMovement => (Component.HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;

    public MoveInputEvent(EntityUid entity, InputMoverComponent component, MoveButtons oldMovement)
    {
        Entity = entity;
        Component = component;
        OldMovement = oldMovement;
    }
}
