using JetBrains.Annotations;
using Robust.Shared.Map;

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

    /// <summary>
    /// Raised on the user before interacting on an entity with bare hand.
    /// Interaction is cancelled if this event is handled, so set it to true if you do custom interaction logic.
    /// </summary>
    public sealed class BeforeInteractHandEvent : HandledEntityEventArgs
    {
        public EntityUid Target { get; }

        public BeforeInteractHandEvent(EntityUid target)
        {
            Target = target;
        }
    }
}
