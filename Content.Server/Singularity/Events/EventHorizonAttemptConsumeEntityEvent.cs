using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Events;

/// <summary>
/// Event raised on the target entity whenever an event horizon attempts to consume an entity.
/// Can be cancelled to prevent the target entity from being consumed.
/// </summary>
public sealed class EventHorizonAttemptConsumeEntityEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The entity that the event horizon is attempting to consume.
    /// </summary>
    public readonly EntityUid Entity;

    /// <summary>
    /// The event horizon consuming the target entity.
    /// </summary>
    public readonly EventHorizonComponent EventHorizon;

    public EventHorizonAttemptConsumeEntityEvent(EntityUid entity, EventHorizonComponent eventHorizon)
    {
        Entity = entity;
        EventHorizon = eventHorizon;
    }
}
