using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// This interface gives components behavior when being clicked on by a user with an object in their hand
    /// who is in range and has unobstructed reach of the target entity (allows inside blockers).
    /// </summary>
    public interface IInteractUsing
    {
        /// <summary>
        /// The interaction priority. Higher numbers get called first.
        /// </summary>
        /// <value>Priority defaults to 0</value>
        int Priority => 0;

        /// <summary>
        /// Called when using one object on another when user is in range of the target entity.
        /// </summary>
        Task<bool> InteractUsing(InteractUsingEventArgs eventArgs);
    }

    public class InteractUsingEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public IEntity User { get; set; }
        public EntityCoordinates ClickLocation { get; set; }
        public IEntity Using { get; set; }
        public IEntity Target { get; set; }
    }

    /// <summary>
    ///     Raised when being clicked on or "attacked" by a user with an object in their hand
    /// </summary>
    [PublicAPI]
    public class InteractUsingMessage : EntitySystemMessage
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
        public IEntity ItemInHand { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity Attacked { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        public InteractUsingMessage(IEntity user, IEntity itemInHand, IEntity attacked, EntityCoordinates clickLocation)
        {
            User = user;
            ItemInHand = itemInHand;
            Attacked = attacked;
            ClickLocation = clickLocation;
        }
    }
}
