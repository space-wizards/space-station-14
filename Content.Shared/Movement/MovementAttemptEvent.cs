using Robust.Shared.GameObjects;

namespace Content.Shared.Movement
{
    public class MovementAttemptEvent : CancellableEntityEventArgs
    {
        public MovementAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
