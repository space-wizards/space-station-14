using JetBrains.Annotations;

namespace Content.Shared.Interaction
{
    public sealed class InteractHandEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public InteractHandEventArgs(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }

        public EntityUid User { get; }
        public EntityUid Target { get; }
    }

    /// <summary>
    ///     Raised directed on a target entity when it is interacted with by a user with an empty hand.
    /// </summary>
    [PublicAPI]
    public sealed class InteractHandEvent : HandledEntityEventArgs, ITargetedInteractEventArgs
    {
        /// <summary>
        ///     Entity that triggered the interaction.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Entity that was interacted on.
        /// </summary>
        public EntityUid Target { get; }

        public InteractHandEvent(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }
    }

    public sealed class InteractNoHandEvent : HandledEntityEventArgs, ITargetedInteractEventArgs
    {
        /// <summary>
        ///     Entity that triggered the interaction.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Entity that was interacted on.
        /// </summary>
        public EntityUid Target { get; }

        public InteractNoHandEvent(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }
    }

    /// <summary>
    /// Reverse of the InteractNoHandEvent - raised on what was interacted on, rather than the other way around.
    /// </summary>
    public sealed class InteractedNoHandEvent : HandledEntityEventArgs, ITargetedInteractEventArgs
    {
        /// <summary>
        /// Entity that was interacted on
        /// </summary>
        public EntityUid Target { get; }

        /// <summary>
        /// Entity that triggered this interaction
        /// </summary>
        public EntityUid User { get; }

        public InteractedNoHandEvent(EntityUid target, EntityUid user)
        {
            Target = target;
            User = user;
        }
    }
}
