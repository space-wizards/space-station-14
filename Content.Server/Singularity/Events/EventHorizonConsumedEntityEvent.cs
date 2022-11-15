using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Events;

/// <summary>
/// Event raised on the event horizon entity whenever an event horizon consumes an entity.
/// </summary>
public sealed class EventHorizonConsumedEntityEvent : EntityEventArgs
{
    /// <summary>
    /// The entity being consumed by the event horizon.
    /// </summary>
    public readonly EntityUid Entity;

    /// <summary>
    /// The event horizon consuming the target entity.
    /// </summary>
    public readonly EventHorizonComponent EventHorizon;

    public EventHorizonConsumedEntityEvent(EntityUid entity, EventHorizonComponent eventHorizon)
    {
        Entity = entity;
        EventHorizon = eventHorizon;
    }
}
