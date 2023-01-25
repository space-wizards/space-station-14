namespace Content.Shared.Storage.Components;

public sealed class InsertIntoEntityStorageAttemptEvent : CancellableEntityEventArgs { }
public sealed class StoreMobInItemContainerAttemptEvent : CancellableEntityEventArgs
{
    public bool Handled = false;
}
public sealed class StorageOpenAttemptEvent : CancellableEntityEventArgs
{
    public bool Silent = false;

    public StorageOpenAttemptEvent (bool silent = false)
    {
        Silent = silent;
    }
}
public sealed class StorageBeforeOpenEvent : EventArgs { }
public sealed class StorageAfterOpenEvent : EventArgs { }
public sealed class StorageCloseAttemptEvent : CancellableEntityEventArgs { }
public sealed class StorageBeforeCloseEvent : EventArgs
{
    public HashSet<EntityUid> Contents;

    /// <summary>
    ///     Entities that will get inserted, regardless of any insertion or whitelist checks.
    /// </summary>
    public HashSet<EntityUid> BypassChecks = new();

    public StorageBeforeCloseEvent(HashSet<EntityUid> contents)
    {
        Contents = contents;
    }
}
public sealed class StorageAfterCloseEvent : EventArgs { }
