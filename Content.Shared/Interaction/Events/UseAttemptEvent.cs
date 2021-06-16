using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class UseAttemptEvent : CancellableEntityEventArgs
    {
        public UseAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class UseAttemptExtensions
    {
        public static bool CanUse(this IEntity entity)
        {
            var ev = new UseAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
