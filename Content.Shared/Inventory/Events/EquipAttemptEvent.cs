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

    public static class EquipAttemptExtensions
    {
        public static bool CanEquip(this IEntity entity)
        {
            var ev = new EquipAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
