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

    public class AfterInteractEventArgs : EventArgs
    {
        public IEntity User { get; }
        public EntityCoordinates ClickLocation { get; }
        public IEntity? Target { get; }
        public bool CanReach { get; }

        public AfterInteractEventArgs(IEntity user, EntityCoordinates clickLocation, IEntity? target, bool canReach)
        {
            User = user;
            ClickLocation = clickLocation;
            Target = target;
            CanReach = canReach;
        }
    }

    /// <summary>
    ///     Raised directed on the used object when clicking on another object and no attack event was handled.
    /// </summary>
    [PublicAPI]
    public class AfterInteractEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that triggered the interaction.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that triggered the interaction.
        /// </summary>
        public EntityUid UserUid => User.Uid;

        /// <summary>
        ///     Entity that the user used to interact.
        /// </summary>
        public IEntity Used { get; }

        /// <summary>
        ///     Entity that the user used to interact.
        /// </summary>
        public EntityUid UsedUid => Used.Uid;

        /// <summary>
        ///     Entity that was interacted on. This can be null if the attack did not click on an entity.
        /// </summary>
        public IEntity? Target { get; }

        /// <summary>
        ///     Entity that was interacted on. This can be null if the attack did not click on an entity.
        /// </summary>
        public EntityUid? TargetUid => Target?.Uid;

        /// <summary>
        ///     Location that the user clicked outside of their interaction range.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        /// <summary>
        /// Is the click location close enough to reach by the player? This does not check for obstructions, just that the target is within
        /// reach radius around the user.
        /// </summary>
        public bool CanReach { get; }

        public AfterInteractEvent(IEntity user, IEntity used, IEntity? target,
            EntityCoordinates clickLocation, bool canReach)
        {
            User = user;
            Used = used;
            Target = target;
            ClickLocation = clickLocation;
            CanReach = canReach;
        }
    }
}
