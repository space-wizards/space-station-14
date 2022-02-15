namespace Content.Shared.Movement
{
    public sealed class MovementAttemptEvent : CancellableEntityEventArgs
    {
        public EntityUid Uid { get; }

        public MovementAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }
    }
}
