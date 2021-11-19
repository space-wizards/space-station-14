using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class UseAttemptEvent : CancellableEntityEventArgs
    {
        public UseAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
