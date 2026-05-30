using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity whenever it has a movement input change.
/// </summary>
[ByRefEvent]
public readonly struct MoveInputEvent(Entity<InputMoverComponent> entity, MoveButtons oldMovement, Direction dir, bool state)
{
    /// <summary>
    /// Mover whose input changed.
    /// </summary>
    public readonly Entity<InputMoverComponent> Entity = entity;

    /// <summary>
    /// Movement buttons held before this input change.
    /// </summary>
    public readonly MoveButtons OldMovement = oldMovement;

    /// <summary>
    /// Discrete direction requested by the current movement buttons.
    /// </summary>
    public readonly Direction Dir = dir;

    /// <summary>
    /// Whether any movement button is held after this input change.
    /// </summary>
    public readonly bool State = state;

    /// <summary>
    /// Whether the current held buttons contain any directional movement.
    /// </summary>
    public bool HasDirectionalMovement => (Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;
}
