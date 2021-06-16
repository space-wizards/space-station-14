using Robust.Shared.GameObjects;

namespace Content.Shared.DragDrop
{
    public class DropAttemptEvent : CancellableEntityEventArgs
    {
        public DropAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class DropAttemptExtensions
    {
        public static bool CanDrop(this IEntity entity)
        {
            var ev = new DropAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
