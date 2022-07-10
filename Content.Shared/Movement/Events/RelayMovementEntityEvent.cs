namespace Content.Shared.Movement.Events
{
    /// <summary>
    /// Raised on a mobmover whenever it tries to move inside a container.
    /// </summary>
    [ByRefEvent]
    public readonly struct RelayMovementEntityEvent
    {
        public readonly EntityUid Entity;

        public RelayMovementEntityEvent(EntityUid entity)
        {
            Entity = entity;
        }
    }
}
