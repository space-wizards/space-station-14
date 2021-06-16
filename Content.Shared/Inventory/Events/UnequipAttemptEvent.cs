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

    public static class UnequipAttemptExtensions
    {
        public static bool CanUnequip(this IEntity entity)
        {
            var ev = new UnequipAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
