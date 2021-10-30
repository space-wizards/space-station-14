using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class AttackAttemptEvent : CancellableEntityEventArgs
    {
        public AttackAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
