using Robust.Shared.Containers;
using Content.Shared.Singularity.Components;

namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// An event queued when an event horizon is contained (put into a container).
/// Exists to delay the event horizon eating its way out of the container until events relating to the insertion have been processed.
/// </summary>
public sealed class EventHorizonContainedEvent : EntityEventArgs {
    /// <summary>
    /// The uid of the event horizon that has been contained.
    /// </summary>
    public readonly EntityUid Entity;

    /// <summary>
    /// The state of the event horizon that has been contained.
    /// </summary>
    public readonly EventHorizonComponent EventHorizon;

    /// <summary>
    /// The arguments of the action that resulted in the event horizon being contained.
    /// </summary>
    public readonly EntGotInsertedIntoContainerMessage Args;

    public EventHorizonContainedEvent(EntityUid entity, EventHorizonComponent eventHorizon, EntGotInsertedIntoContainerMessage args) {
        Entity = entity;
        EventHorizon = eventHorizon;
        Args = args;
    }
}
