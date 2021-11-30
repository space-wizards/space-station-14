using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Events
{
    public class ShiverAttemptEvent : CancellableEntityEventArgs
    {
        public ShiverAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
