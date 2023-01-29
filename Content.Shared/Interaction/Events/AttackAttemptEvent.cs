namespace Content.Shared.Interaction.Events
{
    /// <summary>
    ///     Raised Directed at a user to check whether they are allowed to attack a target.
    /// </summary>
    /// <remarks>
    ///     Combat will also check the general interaction blockers, so this event should only be used for combat-specific
    ///     action blocking.
    /// </remarks>
    public sealed class AttackAttemptEvent : CancellableEntityEventArgs
    {
        public EntityUid Uid { get; }
        public EntityUid? Target { get; }

        public AttackAttemptEvent(EntityUid uid, EntityUid? target = null)
        {
            Uid = uid;
            Target = target;
        }
    }

    /// <summary>
    /// Raised directed at an entity to check if they can attack while inside of a container.
    /// </summary>
    public sealed class CanAttackFromContainerEvent : EntityEventArgs
    {
        public EntityUid Uid;
        public EntityUid? Target;
        public bool CanAttack = false;

        public CanAttackFromContainerEvent(EntityUid uid, EntityUid? target = null)
        {
            Uid = uid;
            Target = target;
        }
    }
}
