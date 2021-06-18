using Robust.Shared.GameObjects;

namespace Content.Shared.DragDrop
{
    public class DropAttemptEvent : CancellableEntityEventArgs
    {
        public DropAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
