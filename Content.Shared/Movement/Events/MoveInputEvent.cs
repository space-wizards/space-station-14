namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity whenever it has a movement input.
/// </summary>
[ByRefEvent]
public readonly struct MoveInputEvent
{
    public readonly EntityUid Entity;

    public MoveInputEvent(EntityUid entity)
    {
        Entity = entity;
    }
}
