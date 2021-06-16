using Robust.Shared.GameObjects;

namespace Content.Shared.Movement
{
    public class MovementAttemptEvent : CancellableEntityEventArgs
    {
        public MovementAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class MovementAttemptExtensions
    {
        public static bool CanMove(this IEntity entity)
        {
            var ev = new MovementAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
