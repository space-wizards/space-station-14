using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Events;

/// <summary>
/// A by-ref event raised on an <paramref name="Entity"/> when an <paramref name="EventHorizon"/> attempts to consume it.
/// Can be cancelled to prevent the target <paramref name="Entity"/> from being consumed.
/// </summary>
/// <param name="EventHorizon">The event horizon that is attempting to consume the <paramref name="Entity"/>.</param>
/// <param name="Entity">The entity that the <paramref name="EventHorizon"/> is attempting to consume.</param>
[ByRefEvent]
public record struct EventHorizonAttemptConsumeEntityEvent(Entity<EventHorizonComponent> EventHorizon, EntityUid Entity)
{
    /// <summary>
    /// May be set to <see langword="true"/> by handlers to prevent <see cref="Entity"/> from being consumed by <see cref="EventHorizon"/>.
    /// </summary>
    public bool Cancelled = false;
}
