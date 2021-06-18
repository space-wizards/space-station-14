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
}
