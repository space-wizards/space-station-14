using Robust.Shared.GameObjects;

namespace Content.Shared.Movement
{
    public sealed class MovementAttemptEvent : CancellableEntityEventArgs
    {
        public MovementAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
