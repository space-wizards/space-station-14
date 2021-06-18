using Robust.Shared.GameObjects;

namespace Content.Shared.Item
{
    public class PickupAttemptEvent : CancellableEntityEventArgs
    {
        public PickupAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
