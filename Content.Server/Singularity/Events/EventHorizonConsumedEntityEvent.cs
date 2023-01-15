using Content.Shared.Singularity.Components;
using Robust.Shared.Containers;

namespace Content.Server.Singularity.Events;

/// <summary>
/// Event raised on the entity being consumed whenever an event horizon consumes an entity.
/// </summary>
public sealed class EventHorizonConsumedEntityEvent : EntityEventArgs
{
    /// <summary>
    /// The entity being consumed by the event horizon.
    /// </summary>
    public readonly EntityUid Entity;

    /// <summary>
    /// The uid of the event horizon consuming the entity.
    /// </summary>
    public readonly EntityUid EventHorizonUid;

    /// <summary>
    /// The event horizon consuming the target entity.
    /// </summary>
    public readonly EventHorizonComponent EventHorizon;

    /// <summary>
    /// The innermost container of the entity being consumed by the event horizon that is not also in the process of being consumed by the event horizon.
    /// Used to correctly dump out the contents containers that are consumed by the event horizon.
    /// </summary>
    public readonly IContainer? Container;

    public EventHorizonConsumedEntityEvent(EntityUid entity, EntityUid eventHorizonUid, EventHorizonComponent eventHorizon, IContainer? container = null)
    {
        Entity = entity;
        EventHorizonUid = eventHorizonUid;
        EventHorizon = eventHorizon;
        Container = container;
    }
}
