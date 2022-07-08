namespace Content.Server.Ghost.Roles.Events
{
    public sealed class GhostRoleSpawnerUsedEvent : EntityEventArgs
    {
        /// <summary>
        /// The entity that spawned this.
        /// </summary>
        public EntityUid Spawner = new();

        /// <summary>
        /// The entity spawned.
        /// </summary>
        public EntityUid Spawned = new();

        public GhostRoleSpawnerUsedEvent(EntityUid spawner, EntityUid spawned)
        {
            Spawner = spawner;

            Spawned = spawned;
        }
    }
}
