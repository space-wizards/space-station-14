namespace Content.Shared.Movement.Events
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
