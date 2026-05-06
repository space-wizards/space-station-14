namespace Content.Shared.Chasm.Events;

/// <summary>
/// An event raised on a chasm that does something with the entity that fell into it.
/// </summary>
/// <param name="Entity">The entity that fell into the chasm.</param>
[ByRefEvent]
public record struct ChasmFallEffectsEvent(EntityUid Entity);
