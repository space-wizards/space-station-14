using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class ChangeDirectionAttemptEvent : CancellableEntityEventArgs
    {
        public ChangeDirectionAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class ChangeDirectionAttemptExtensions
    {
        public static bool CanChangeDirection(this IEntity entity)
        {
            var ev = new ChangeDirectionAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
