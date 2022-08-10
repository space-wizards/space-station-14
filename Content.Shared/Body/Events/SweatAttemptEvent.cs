namespace Content.Shared.Body.Events
{
    public sealed class SweatAttemptEvent : CancellableEntityEventArgs
    {
        public SweatAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
