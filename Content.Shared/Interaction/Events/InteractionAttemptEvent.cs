using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class InteractionAttemptEvent : CancellableEntityEventArgs
    {
        public InteractionAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
