using Robust.Shared.GameObjects;

namespace Content.Shared.Emoting
{
    public class EmoteAttemptEvent : CancellableEntityEventArgs
    {
        public EmoteAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
