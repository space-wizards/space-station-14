using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;

namespace Content.Server.Singularity.Events;

/// <summary>
///     Event raised on the event horizon entity whenever an event horizon consumes an entity.
/// </summary>
public sealed class EntityConsumedByEventHorizonEvent : EntityEventArgs
{
    /// <summary>
    ///     The entity being consumed by the event horizon.
    /// </summary>
    public readonly EntityUid Entity;

    /// <summary>
    ///     The event horizon consuming the entity.
    /// </summary>
    public readonly SharedEventHorizonComponent EventHorizon;

    /// <summary>
    ///     The local event horizon system.
    /// </summary>
    public readonly SharedEventHorizonSystem EventHorizonSystem;

    public EntityConsumedByEventHorizonEvent(EntityUid entity, SharedEventHorizonComponent eventHorizon, SharedEventHorizonSystem eventHorizonSystem)
    {
        Entity = entity;
        EventHorizon = eventHorizon;
        EventHorizonSystem = eventHorizonSystem;
    }
}
