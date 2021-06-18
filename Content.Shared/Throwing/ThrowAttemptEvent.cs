using Robust.Shared.GameObjects;

namespace Content.Shared.Throwing
{
    public class ThrowAttemptEvent : CancellableEntityEventArgs
    {
        public ThrowAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
