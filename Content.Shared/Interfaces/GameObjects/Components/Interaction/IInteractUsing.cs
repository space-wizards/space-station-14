using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// This interface gives components behavior when their entity is clicked on by a user with an object in their hand
    /// who is in range and has unobstructed reach of the target entity (allows inside blockers). This includes
    /// clicking on an object in the world as well as clicking on an object in inventory.
    /// </summary>
    [RequiresExplicitImplementation]
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
        [Obsolete("Use InteractUsingMessage instead")]
        Task<bool> InteractUsing(InteractUsingEventArgs eventArgs);
    }

    public class InteractUsingEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public InteractUsingEventArgs(IEntity user, EntityCoordinates clickLocation, IEntity @using, IEntity target)
        {
            User = user;
            ClickLocation = clickLocation;
            Using = @using;
            Target = target;
        }

        public IEntity User { get; }
        public EntityCoordinates ClickLocation { get; }
        public IEntity Using { get; }
        public IEntity Target { get; }
    }

    /// <summary>
    ///     Raised when being clicked on or "attacked" by a user with an object in their hand
    /// </summary>
    [PublicAPI]
    public class InteractUsingMessage : HandledEntityEventArgs
    {
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
