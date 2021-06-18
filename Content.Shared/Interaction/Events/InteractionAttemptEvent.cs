using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class InteractionAttemptEvent : CancellableEntityEventArgs
    {
        public InteractionAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
