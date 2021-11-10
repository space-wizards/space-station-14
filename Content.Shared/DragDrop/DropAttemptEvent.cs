using Robust.Shared.GameObjects;

namespace Content.Shared.DragDrop
{
    public class DropAttemptEvent : CancellableEntityEventArgs
    {
        public DropAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
