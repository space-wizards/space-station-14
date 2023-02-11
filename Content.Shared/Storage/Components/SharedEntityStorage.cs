namespace Content.Shared.Storage.Components;

[ByRefEvent]
public record struct InsertIntoEntityStorageAttemptEvent(bool Cancelled = false);

[ByRefEvent]
public record struct StoreMobInItemContainerAttemptEvent(bool Handled, bool Cancelled = false);

[ByRefEvent]
public record struct StorageOpenAttemptEvent(bool Silent, bool Cancelled = false);

[ByRefEvent]
public readonly record struct StorageBeforeOpenEvent;

[ByRefEvent]
public readonly record struct StorageAfterOpenEvent;

[ByRefEvent]
public record struct StorageCloseAttemptEvent(bool Cancelled = false);

[ByRefEvent]
public readonly record struct StorageBeforeCloseEvent(HashSet<EntityUid> Contents, HashSet<EntityUid> BypassChecks);

[ByRefEvent]
public readonly record struct StorageAfterCloseEvent;
