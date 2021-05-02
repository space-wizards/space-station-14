using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when being activated (by default,
    ///     this is done via the "E" key) when the user is in range and has unobstructed access to the target entity
    ///     (allows inside blockers). This includes activating an object in the world as well as activating an
    ///     object in inventory. Unlike IUse, this can be performed on entities that aren't in the active hand,
    ///     even when the active hand is currently holding something else.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IActivate
    {
        /// <summary>
        ///     Called when this component is activated by another entity who is in range.
        /// </summary>
        [Obsolete("Use ActivateInWorldMessage instead")]
        void Activate(ActivateEventArgs eventArgs);
    }

    public class ActivateEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public ActivateEventArgs(IEntity user, IEntity target)
        {
            User = user;
            Target = target;
        }

        public IEntity User { get; }
        public IEntity Target { get; }
    }

    /// <summary>
    ///     Raised when an entity is activated in the world.
    /// </summary>
    [PublicAPI]
    public class ActivateInWorldMessage : HandledEntityEventArgs
    {
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
