using Content.Shared.Singularity.Components;
using Robust.Shared.Containers;

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
    public readonly EventHorizonComponent EventHorizon;

    /// <summary>
    /// The innermost container of the entity being consumed by the event horizon that is not also in the process of being consumed by the event horizon.
    /// Used to correctly dump out the contents containers that are consumed by the event horizon.
    /// </summary>
    public readonly IContainer? Container;

    public EntityConsumedByEventHorizonEvent(EntityUid entity, EventHorizonComponent eventHorizon, IContainer? container = null)
    {
        Entity = entity;
        EventHorizon = eventHorizon;
        Container = container;
    }
}
