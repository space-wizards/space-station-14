using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events
{
    public class EquipAttemptEvent : CancellableEntityEventArgs
    {
        public EquipAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
