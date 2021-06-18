using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events
{
    public class UnequipAttemptEvent : CancellableEntityEventArgs
    {
        public UnequipAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
