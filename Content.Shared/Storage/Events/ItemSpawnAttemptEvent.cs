namespace Content.Shared.Storage.Events
{
    /// <summary>
    /// Raised when SpawnItemsOnUse system tries to spawn an item.
    /// </summary>
    [ByRefEvent]
    public sealed class ItemSpawnAttemptEvent : CancellableEntityEventArgs;

}
