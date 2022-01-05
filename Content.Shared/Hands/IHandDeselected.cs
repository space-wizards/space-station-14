using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Hands
{
    /// <summary>
    ///     This interface gives components behavior when they're held on a deselected hand.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IHandDeselected
    {
        [Obsolete("Use HandDeselectedMessage instead")]
        void HandDeselected(HandDeselectedEventArgs eventArgs);
    }

    public class HandDeselectedEventArgs : EventArgs
    {
        public HandDeselectedEventArgs(EntityUid user)
        {
            User = user;
        }

        public EntityUid User { get; }
    }

    /// <summary>
    ///     Raised when an entity item in a hand is deselected.
    /// </summary>
    [PublicAPI]
    public class HandDeselectedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that owns the deselected hand.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Item in the hand that was deselected.
        /// </summary>
        public EntityUid Item { get; }

        public HandDeselectedEvent(EntityUid user, EntityUid item)
        {
            User = user;
            Item = item;
        }
    }
}
