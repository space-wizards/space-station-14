using Robust.Shared.GameObjects;

namespace Content.Shared.Item
{
    /// <summary>
    /// Raised on a *mob* when it tries to pickup something
    /// </summary>
    public sealed class PickupAttemptEvent : CancellableEntityEventArgs
    {
        public PickupAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }

    /// <summary>
    /// Raised on the *item* when tried to be picked up
    /// </summary>
    /// <remarks>
    /// Doesn't just handle "items" but calling it "PickedUpAttempt" is too close to "Pickup" for the sleep deprived brain.
    /// </remarks>
    public sealed class AttemptItemPickupEvent : CancellableEntityEventArgs
    {
    }
}
