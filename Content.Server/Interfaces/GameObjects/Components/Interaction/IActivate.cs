using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    /// <summary>
    ///     This interface gives components behavior when being activated in the world when the user
    ///     is in range and has unobstructed access to the target entity (allows inside blockers).
    /// </summary>
    public interface IActivate
    {
        /// <summary>
        ///     Called when this component is activated by another entity who is in range.
        /// </summary>
        void Activate(ActivateEventArgs eventArgs);
    }

    public class ActivateEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public IEntity User { get; set; }
        public IEntity Target { get; set; }
    }

    /// <summary>
    ///     Raised when an entity is activated in the world.
    /// </summary>
    [PublicAPI]
    public class ActivateInWorldMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that activated the world entity.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that was activated in the world.
        /// </summary>
        public IEntity Activated { get; }

        public ActivateInWorldMessage(IEntity user, IEntity activated)
        {
            User = user;
            Activated = activated;
        }
    }
}
