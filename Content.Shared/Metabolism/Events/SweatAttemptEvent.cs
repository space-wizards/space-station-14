using Robust.Shared.GameObjects;

namespace Content.Shared.Metabolism.Events
{
    public class SweatAttemptEvent : CancellableEntityEventArgs
    {
        public SweatAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class SweatAttemptExtensions
    {
        public static bool CanSweat(this IEntity entity)
        {
            var ev = new SweatAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
