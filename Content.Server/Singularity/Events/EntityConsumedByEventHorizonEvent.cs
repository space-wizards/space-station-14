using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Events;

/// <summary>
///     Event raised on the event horizon entity whenever an event horizon consumes an entity.
/// </summary>
public sealed class EntityConsumedByEventHorizonEvent : EntityEventArgs
{
    /// <summary>
    /// The entity being consumed by the event horizon.
    /// </summary>
    public readonly EntityUid Entity;

    /// <summary>
    /// The event horizon consuming the entity.
    /// </summary>
    public readonly SharedEventHorizonComponent EventHorizon;

    public EntityConsumedByEventHorizonEvent(EntityUid entity, SharedEventHorizonComponent eventHorizon)
    {
        Entity = entity;
        EventHorizon = eventHorizon;
    }
}
