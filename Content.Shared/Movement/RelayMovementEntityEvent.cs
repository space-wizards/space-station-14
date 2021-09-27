using Robust.Shared.GameObjects;

namespace Content.Shared.Movement
{
    public sealed class RelayMovementEntityEvent : EntityEventArgs
    {
        public IEntity Entity { get; }

        public RelayMovementEntityEvent(IEntity entity)
        {
            Entity = entity;
        }
    }
}
