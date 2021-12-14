using Robust.Shared.GameObjects;

namespace Content.Shared.Movement
{
    public sealed class RelayMovementEntityEvent : EntityEventArgs
    {
        public EntityUid Entity { get; }

        public RelayMovementEntityEvent(EntityUid entity)
        {
            Entity = entity;
        }
    }
}
