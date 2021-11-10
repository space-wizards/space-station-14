using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events
{
    public class UnequipAttemptEvent : CancellableEntityEventArgs
    {
        public UnequipAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
