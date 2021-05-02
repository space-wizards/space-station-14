using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// This interface gives components behavior when being clicked on by a user with an empty hand
    /// who is in range and has unobstructed reach of the target entity (allows inside blockers).
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IInteractHand
    {
        /// <summary>
        /// Called when a player directly interacts with an empty hand when user is in range of the target entity.
        /// </summary>
        [Obsolete("Use AttackHandMessage instead")]
        bool InteractHand(InteractHandEventArgs eventArgs);
    }

    public class InteractHandEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public InteractHandEventArgs(IEntity user, IEntity target)
        {
            User = user;
            Target = target;
        }

        public IEntity User { get; }
        public IEntity Target { get; }
    }


    /// <summary>
    ///      Raised when being clicked on or "attacked" by a user with an empty hand.
    /// </summary>
    [PublicAPI]
    public class AttackHandMessage : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity Attacked { get; }

        public AttackHandMessage(IEntity user, IEntity attacked)
        {
            User = user;
            Attacked = attacked;
        }
    }
}
