using Robust.Shared.Players;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity whenever it has a movement input.
/// </summary>
[ByRefEvent]
public readonly struct MoveInputEvent
{
    public readonly EntityUid Entity;
    public readonly Direction Dir;
    public readonly bool State;

    public MoveInputEvent(EntityUid entity, Direction dir, bool state)
    {
        Entity = entity;
        Dir = dir;
        State = state;
    }
}
