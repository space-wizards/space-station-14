using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Metabolism
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
