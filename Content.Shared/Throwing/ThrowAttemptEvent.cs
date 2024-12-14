namespace Content.Shared.Throwing
{
    public sealed class ThrowAttemptEvent : CancellableEntityEventArgs
    {
        public ThrowAttemptEvent(EntityUid uid, EntityUid itemUid)
        {
            Uid = uid;
            ItemUid = itemUid;
        }

        public EntityUid Uid { get; }

        public EntityUid ItemUid { get; }
    }

    /// <summary>
    ///     Raised on the item entity that is thrown.
    /// </summary>
    /// <param name="User">The user that threw this entity.</param>
    /// <param name="Cancelled">Whether or not the throw should be cancelled.</param>
    [ByRefEvent]
    public record struct ThrowItemAttemptEvent(EntityUid User, bool Cancelled = false);

    /// <summary>
    /// Raised when we try to pushback an entity from throwing
    /// </summary>
    public sealed class ThrowPushbackAttemptEvent : CancellableEntityEventArgs {}
}
