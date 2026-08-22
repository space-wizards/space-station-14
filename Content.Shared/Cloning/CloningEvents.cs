namespace Content.Shared.Cloning.Events;

/// <summary>
/// Raised before a mob is cloned. Cancel to prevent cloning.
/// This is raised on the original mob.
/// </summary>
[ByRefEvent]
public record struct CloningAttemptEvent(CloningSettingsPrototype Settings, bool Cancelled = false);

/// <summary>
/// Raised after an entity was cloned from an original.
/// This is raised on the original entity, and the cloned entity has been initialized and started.
/// This SHOULD NOT copy data from the original. Do that in CloningContext instead.
/// </summary>
[ByRefEvent]
public record struct ClonedEvent(EntityUid CloneUid, CloningSettingsPrototype Settings);
