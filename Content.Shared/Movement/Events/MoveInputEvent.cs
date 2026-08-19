using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity before it processes a movement input change.
/// </summary>
[ByRefEvent]
public record struct BeforeMoverMoveEvent
{
    public bool Handled;
}

/// <summary>
/// Raised on an entity whenever it has a movement input change.
/// </summary>
[ByRefEvent]
public readonly struct MoveInputEvent(Entity<InputMoverComponent> entity, MoveButtons oldMovement)
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
    /// Whether the current held buttons contain any directional movement.
    /// </summary>
    public bool HasDirectionalMovement => (Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;
}
