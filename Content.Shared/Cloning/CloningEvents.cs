namespace Content.Shared.Cloning.Events;

/// <summary>
///    Raised before a mob is cloned. Cancel to prevent cloning.
/// </summary>
[ByRefEvent]
public record struct CloningAttemptEvent(CloningSettingsPrototype Settings, bool Cancelled = false);

/// <summary>
///    Raised after a new mob got spawned when cloning a humanoid.
/// </summary>
[ByRefEvent]
public record struct CloningEvent(CloningSettingsPrototype Settings, EntityUid CloneUid);
