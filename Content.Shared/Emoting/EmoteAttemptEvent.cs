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
}
