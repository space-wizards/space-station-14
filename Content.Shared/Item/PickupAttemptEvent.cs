using Robust.Shared.GameObjects;

namespace Content.Shared.Item
{
    /// <summary>
    /// Raised on a *mob* when it tries to pickup something
    /// </summary>
    public class PickupAttemptEvent : CancellableEntityEventArgs
    {
        public PickupAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
