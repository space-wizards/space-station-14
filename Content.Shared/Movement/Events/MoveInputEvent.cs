using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity before it processes a movement input change.
/// </summary>
[ByRefEvent]
public record struct BeforeMoveEvent
{
    public bool Handled;
}

/// <summary>
/// Raised on an entity whenever it has a movement input change.
/// </summary>
[ByRefEvent]
public readonly struct MoveInputEvent(Entity<InputMoverComponent> entity, MoveButtons oldMovement, Vector2 moveVec)
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
    /// Normalized direction vector requested by the current movement buttons.
    /// </summary>
    public readonly Vector2 MoveVec = moveVec;

    /// <summary>
    /// Whether the current held buttons contain any directional movement.
    /// </summary>
    public bool HasDirectionalMovement => (Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;
}
