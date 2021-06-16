using Robust.Shared.GameObjects;

namespace Content.Shared.Metabolism.Events
{
    public class ShiverAttemptEvent : CancellableEntityEventArgs
    {
        public ShiverAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class ShiverAttemptExtensions
    {
        public static bool CanShiver(this IEntity entity)
        {
            var ev = new ShiverAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
