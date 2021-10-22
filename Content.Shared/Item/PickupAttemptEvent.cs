using Robust.Shared.GameObjects;

namespace Content.Shared.Item
{
    /// <summary>
    /// Raised on a *mob* when it tries to pickup something
    /// </summary>
    public class PickupAttemptEvent : CancellableEntityEventArgs
    {
        public PickupAttemptEvent(EntityUid entity)
        {
            Entity = entity;
        }

        public EntityUid Entity { get; }
    }


    /// <summary>
    /// Raised on the *item* that's being picked up
    /// </summary>
    public class PickedUpAttemptEvent : CancellableEntityEventArgs
    {
        public PickedUpAttemptEvent(EntityUid entity)
        {
            Entity = entity;
        }

        public EntityUid Entity { get; }
    }
}
