using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Events;

/// <summary>
/// Event raised on the target entity whenever an event horizon attempts to consume an entity.
/// Can be cancelled to prevent the target entity from being consumed.
/// </summary>
[ByRefEvent]
public record struct EventHorizonAttemptConsumeEntityEvent
(EntityUid entity, EntityUid eventHorizonUid, EventHorizonComponent eventHorizon)
{
    /// <summary>
    /// The entity that the event horizon is attempting to consume.
    /// </summary>
    public readonly EntityUid Entity = entity;

    /// <summary>
    /// The uid of the event horizon consuming the entity.
    /// </summary>
    public readonly EntityUid EventHorizonUid = eventHorizonUid;

    /// <summary>
    /// The event horizon consuming the target entity.
    /// </summary>
    public readonly EventHorizonComponent EventHorizon = eventHorizon;

    /// <summary>
    /// Whether the event horizon has been prevented from consuming the target entity.
    /// </summary>
    public bool Cancelled = false;
}
