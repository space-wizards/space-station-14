using Robust.Shared.GameObjects;

namespace Content.Shared.Throwing
{
    public class ThrowAttemptEvent : CancellableEntityEventArgs
    {
        public ThrowAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class ThrowAttemptExtensions
    {
        public static bool CanThrow(this IEntity entity)
        {
            var ev = new ThrowAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
