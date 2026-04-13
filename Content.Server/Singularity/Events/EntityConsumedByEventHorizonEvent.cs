using Content.Shared.Singularity.Components;
using Robust.Shared.Containers;

namespace Content.Server.Singularity.Events;

/// <summary>
/// A by-ref event raised on a <paramref name="EventHorizon"/> when it consumes an <paramref name="Entity"/>.
/// </summary>
/// <param name="EventHorizon">The event horizon consuming the <paramref name="Entity"/>.</param>
/// <param name="Entity">The entity being consumed by the <paramref name="EventHorizon"/>.</param>
/// <param name="OuterContainer">If the entity is being consumed because its container is being consumed, this is the outermost container that isn't being consumed.</param>
[ByRefEvent]
public readonly record struct EntityConsumedByEventHorizonEvent(Entity<EventHorizonComponent> EventHorizon, EntityUid Entity, BaseContainer? OuterContainer);
