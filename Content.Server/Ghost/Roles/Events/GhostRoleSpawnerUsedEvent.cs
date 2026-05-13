namespace Content.Server.Ghost.Roles.Events
{
    /// <summary>
    /// Raised on a spawned entity after they use a ghost role mob spawner.
    /// </summary>
    public sealed partial class GhostRoleSpawnerUsedEvent : EntityEventArgs
    {
        /// <summary>
        /// The entity that spawned this.
        /// </summary>
        public EntityUid Spawner;

        /// <summary>
        /// The entity spawned.
        /// </summary>
        public EntityUid Spawned;

        public GhostRoleSpawnerUsedEvent(EntityUid spawner, EntityUid spawned)
        {
            Spawner = spawner;

            Spawned = spawned;
        }
    }
}

