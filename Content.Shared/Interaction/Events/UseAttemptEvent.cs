using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class UseAttemptEvent : CancellableEntityEventArgs
    {
        public UseAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
