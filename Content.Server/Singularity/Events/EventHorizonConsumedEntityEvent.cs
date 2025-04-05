using Content.Shared.Singularity.Components;
using Robust.Shared.Containers;

namespace Content.Server.Singularity.Events;

/// <summary>
/// A by-ref event raised on an <paramref name="Entity"/> when it is consumed by an <paramref name="EventHorizon"/>.
/// </summary>
/// <param name="EventHorizon">The event horizon that is consuming the <paramref name="Entity"/>.</param>
/// <param name="Entity">The entity that is being consumed by the <paramref name="EventHorizon"/>.</param>
/// <param name="OuterContainer">If the entity is being consumed because its container is being consumed, this is the outermost container that isn't being consumed.</param>
[ByRefEvent]
public readonly record struct EventHorizonConsumedEntityEvent(Entity<EventHorizonComponent> EventHorizon, EntityUid Entity, BaseContainer? OuterContainer);
