using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
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
    ///     Raised when being clicked by objects outside the range of direct use.
    /// </summary>
    [PublicAPI]
    public class RangedInteractMessage : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that the User attacked with.
        /// </summary>
        public IEntity ItemInHand { get; set; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity Attacked { get; }

        /// <summary>
        ///     Location that the user clicked outside of their interaction range.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        public RangedInteractMessage(IEntity user, IEntity itemInHand, IEntity attacked, EntityCoordinates clickLocation)
        {
            User = user;
            ItemInHand = itemInHand;
            ClickLocation = clickLocation;
            Attacked = attacked;
        }
    }
}
