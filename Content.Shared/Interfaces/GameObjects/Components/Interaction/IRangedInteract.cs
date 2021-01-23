using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
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
        /// <returns></returns>
        bool RangedInteract(RangedInteractEventArgs eventArgs);
    }

    [PublicAPI]
    public class RangedInteractEventArgs : EventArgs
    {
        public IEntity User { get; set; }
        public IEntity Using { get; set; }
        public EntityCoordinates ClickLocation { get; set; }
    }

    /// <summary>
    ///     Raised when being clicked by objects outside the range of direct use.
    /// </summary>
    [PublicAPI]
    public class RangedInteractMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

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
