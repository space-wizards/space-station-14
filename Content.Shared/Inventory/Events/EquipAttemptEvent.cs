using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events
{
    public class EquipAttemptEvent : CancellableEntityEventArgs
    {
        public EquipAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
