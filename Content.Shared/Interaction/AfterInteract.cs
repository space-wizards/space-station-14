using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Map;


namespace Content.Shared.Interaction
{
    [PublicAPI]
    public abstract class InteractEvent : HandledEntityEventArgs
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
        ///     Entity that was interacted on. This can be null if there was no target (e.g., clicking on tiles).
        /// </summary>
        public EntityUid? Target { get; }

        /// <summary>
        ///     Location that the user clicked outside of their interaction range.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        /// <summary>
        /// Is the click location in range without obstructions?
        /// </summary>
        public bool CanReach { get; }

        public InteractEvent(EntityUid user, EntityUid used, EntityUid? target,
            EntityCoordinates clickLocation, bool canReach)
        {
            User = user;
            Used = used;
            Target = target;
            ClickLocation = clickLocation;
            CanReach = canReach;
        }
    }

    /// <summary>
    ///     Raised directed on the used object when clicking on another object and no standard interaction occurred.
    ///     Used for low-priority interactions facilitated by the used entity.
    /// </summary>
    public sealed class AfterInteractEvent : InteractEvent
    {
        public AfterInteractEvent(EntityUid user, EntityUid used, EntityUid? target,
            EntityCoordinates clickLocation, bool canReach) : base(user, used, target, clickLocation, canReach)
        { }
    }

    /// <summary>
    ///     Raised directed on the target when clicking on another object and no standard interaction occurred. Used for
    ///     low-priority interactions facilitated by the target entity.
    /// </summary>
    public sealed class AfterInteractUsingEvent : InteractEvent
    {
        public AfterInteractUsingEvent(EntityUid user, EntityUid used, EntityUid? target,
            EntityCoordinates clickLocation, bool canReach) : base(user, used, target, clickLocation, canReach)
        { }
    }
}
