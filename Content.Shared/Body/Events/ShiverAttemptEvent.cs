using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Events
{
    public class ShiverAttemptEvent : CancellableEntityEventArgs
    {
        public ShiverAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
