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
}
