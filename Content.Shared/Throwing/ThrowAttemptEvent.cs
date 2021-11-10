using Robust.Shared.GameObjects;

namespace Content.Shared.Throwing
{
    public class ThrowAttemptEvent : CancellableEntityEventArgs
    {
        public ThrowAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }

    /// <summary>
    /// Raised when we try to pushback an entity from throwing
    /// </summary>
    public sealed class ThrowPushbackAttemptEvent : CancellableEntityEventArgs {}
}
