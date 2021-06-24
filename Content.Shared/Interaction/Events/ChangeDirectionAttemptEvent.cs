using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class ChangeDirectionAttemptEvent : CancellableEntityEventArgs
    {
        public ChangeDirectionAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
