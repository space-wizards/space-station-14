namespace Content.Shared.Body.Events
{
    public sealed class ShiverAttemptEvent : CancellableEntityEventArgs
    {
        public ShiverAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
