using Robust.Shared.GameObjects;

namespace Content.Shared.Item
{
    /// <summary>
    /// Raised on a *mob* when it tries to pickup something
    /// </summary>
    public class PickupAttemptEvent : CancellableEntityEventArgs
    {
        public PickupAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }


    /// <summary>
    /// Raised on the *item* that's being picked up
    /// </summary>
    public class PickedUpAttemptEvent : CancellableEntityEventArgs
    {
        public PickedUpAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
