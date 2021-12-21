using Robust.Shared.GameObjects;

namespace Content.Shared.Item
{
    public class PickupAttemptEvent : CancellableEntityEventArgs
    {
        public PickupAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
