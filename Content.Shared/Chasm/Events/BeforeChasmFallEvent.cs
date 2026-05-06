namespace Content.Shared.Chasm.Events;

/// <summary>
/// Raised on an entity that already fell into a chasm in order to
/// prevent the effects of the chasm in the last moment.
/// </summary>
[ByRefEvent]
public record struct BeforeChasmFallEvent(EntityUid? Chasm, bool Cancelled = false);
