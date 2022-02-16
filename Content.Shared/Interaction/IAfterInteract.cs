using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;


namespace Content.Shared.Interaction
{
    /// <summary>
    /// This interface gives components a behavior when their entity is in the active hand, when
    /// clicking on another object and no interaction occurs, at any range. This includes
    /// clicking on an object in the world as well as clicking on an object in inventory.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IAfterInteract
    {
        /// <summary>
        /// The interaction priority. Higher numbers get called first.
        /// </summary>
        /// <value>Priority defaults to 0</value>
        int Priority => 0;

        /// <summary>
        /// Called when we interact with nothing, or when we interact with an entity out of range that has no behavior
        /// </summary>
        [Obsolete("Use AfterInteractMessage instead")]
        Task<bool> AfterInteract(AfterInteractEventArgs eventArgs);
    }

    public sealed class AfterInteractEventArgs : EventArgs
    {
        public EntityUid User { get; }
        public EntityCoordinates ClickLocation { get; }
        public EntityUid? Target { get; }
        public bool CanReach { get; }

        public AfterInteractEventArgs(EntityUid user, EntityCoordinates clickLocation, EntityUid? target, bool canReach)
        {
            User = user;
            ClickLocation = clickLocation;
            Target = target;
            CanReach = canReach;
        }
    }

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
