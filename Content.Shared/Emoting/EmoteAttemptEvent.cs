using Robust.Shared.GameObjects;

namespace Content.Shared.Emoting
{
    public class EmoteAttemptEvent : CancellableEntityEventArgs
    {
        public EmoteAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class EmoteAttemptExtensions
    {
        public static bool CanEmote(this IEntity entity)
        {
            var ev = new EmoteAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
