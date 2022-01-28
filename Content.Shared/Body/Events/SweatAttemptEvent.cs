using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Events
{
    public class SweatAttemptEvent : CancellableEntityEventArgs
    {
        public SweatAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
