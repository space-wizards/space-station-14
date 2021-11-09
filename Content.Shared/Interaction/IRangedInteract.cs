using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interaction
{
    /// <summary>
    /// This interface gives components behavior when being clicked on by a user with an object
    /// outside the range of direct use
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IRangedInteract
    {
        /// <summary>
        /// Called when we try to interact with an entity out of range
        /// </summary>
        [Obsolete("Use RangedInteractMessage instead")]
        bool RangedInteract(RangedInteractEventArgs eventArgs);
    }

    [PublicAPI]
    public class RangedInteractEventArgs : EventArgs
    {
        public RangedInteractEventArgs(IEntity user, IEntity @using, EntityCoordinates clickLocation)
        {
            User = user;
            Using = @using;
            ClickLocation = clickLocation;
        }

        public IEntity User { get; }
        public IEntity Using { get; }
        public EntityCoordinates ClickLocation { get; }
    }

    /// <summary>
    ///     Raised when an entity is interacted with that is out of the user entity's range of direct use.
    /// </summary>
    [PublicAPI]
    public class RangedInteractEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that triggered the interaction.
        /// </summary>
        public EntityUid UserUid { get; }

        /// <summary>
        ///     Entity that the user used to interact.
        /// </summary>
        public EntityUid UsedUid { get; }

        /// <summary>
        ///     Entity that was interacted on.
        /// </summary>
        public EntityUid TargetUid { get; }

        /// <summary>
        ///     Location that the user clicked outside of their interaction range.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        public RangedInteractEvent(EntityUid user, EntityUid used, EntityUid target, EntityCoordinates clickLocation)
        {
            UserUid = user;
            UsedUid = used;
            TargetUid = target;
            ClickLocation = clickLocation;
        }
    }
}
