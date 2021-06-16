using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class AttackAttemptEvent : CancellableEntityEventArgs
    {
        public AttackAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class AttackAttemptExtensions
    {
        public static bool CanAttack(this IEntity entity)
        {
            var ev = new AttackAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
