using Robust.Shared.GameObjects;

namespace Content.Shared.Speech
{
    public class SpeakAttemptEvent : CancellableEntityEventArgs
    {
        public SpeakAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class SpeakAttemptExtensions
    {
        public static bool CanSpeak(this IEntity entity)
        {
            var ev = new SpeakAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
