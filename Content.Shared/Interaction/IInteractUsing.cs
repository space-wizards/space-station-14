using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interaction
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
        public InteractUsingEventArgs(EntityUid user, EntityCoordinates clickLocation, EntityUid @using, EntityUid target)
        {
            User = user;
            ClickLocation = clickLocation;
            Using = @using;
            Target = target;
        }

        public EntityUid User { get; }
        public EntityCoordinates ClickLocation { get; }
        public EntityUid Using { get; }
        public EntityUid Target { get; }
    }

    /// <summary>
    ///     Raised when a target entity is interacted with by a user while holding an object in their hand.
    /// </summary>
    [PublicAPI]
    public class InteractUsingEvent : HandledEntityEventArgs
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
        ///     Entity that was interacted on.
        /// </summary>
        public EntityUid Target { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        public InteractUsingEvent(EntityUid user, EntityUid used, EntityUid target, EntityCoordinates clickLocation)
        {
            User = user;
            Used = used;
            Target = target;
            ClickLocation = clickLocation;
        }
    }
}
