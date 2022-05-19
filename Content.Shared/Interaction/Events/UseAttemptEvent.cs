namespace Content.Shared.Interaction.Events
{
    public sealed class UseAttemptEvent : CancellableEntityEventArgs
    {
        public UseAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
