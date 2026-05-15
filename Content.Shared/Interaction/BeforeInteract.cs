using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Interaction
{
    /// <summary>
    ///     Raised directed on the used object when clicking on another object before an interaction is handled.
    /// </summary>
    [PublicAPI]
    public sealed class BeforeRangedInteractEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that triggered the interaction.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Entity that the user used to interact.
        /// </summary>
        public EntityUid Used { get; }

        /// <summary>
        ///     Entity that was interacted on. This can be null if the attack did not click on an entity.
        /// </summary>
        public EntityUid? Target { get; }

        /// <summary>
        ///     Location that the user clicked outside of their interaction range.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        /// <summary>
        /// Is the click location in range and unobstructed?
        /// </summary>
        public bool CanReach { get; }

        public BeforeRangedInteractEvent(
            EntityUid user,
            EntityUid used,
            EntityUid? target,
            EntityCoordinates clickLocation,
            bool canReach)
        {
            User = user;
            Used = used;
            Target = target;
            ClickLocation = clickLocation;
            CanReach = canReach;
        }
    }
}
