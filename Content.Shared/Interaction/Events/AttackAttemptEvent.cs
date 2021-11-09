using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class AttackAttemptEvent : CancellableEntityEventArgs
    {
        public AttackAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
