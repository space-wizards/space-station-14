namespace Content.Shared.DragDrop
{
    public sealed class DropAttemptEvent : CancellableEntityEventArgs
    {
        public DropAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
