namespace Content.Shared.Interaction.Events
{
    /// <summary>
    ///     Event raised directed at a user to see if they can perform a generic interaction.
    /// </summary>
    public sealed class InteractionAttemptEvent : CancellableEntityEventArgs
    {
        public InteractionAttemptEvent(EntityUid uid, EntityUid? target)
        {
            Uid = uid;
            Target = target;
        }

        public EntityUid Uid { get; }
        public EntityUid? Target { get; }
    }

    /// <summary>
    ///     Event raised directed at the target entity of an interaction to see if the user is allowed to perform some
    ///     generic interaction.
    /// </summary>
    public sealed class GettingInteractedWithAttemptEvent : CancellableEntityEventArgs
    {
        public GettingInteractedWithAttemptEvent(EntityUid uid, EntityUid? target)
        {
            Uid = uid;
            Target = target;
        }

        public EntityUid Uid { get; }
        public EntityUid? Target { get; }
    }
}
