using Robust.Shared.GameObjects;

namespace Content.Shared.Item
{
    public class PickupAttemptEvent : CancellableEntityEventArgs
    {
        public PickupAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class PickupAttemptExtensions
    {
        public static bool CanPickup(this IEntity entity)
        {
            var ev = new PickupAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
