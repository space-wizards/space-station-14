using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Metabolism
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
