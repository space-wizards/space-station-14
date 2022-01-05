using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Hands
{
    /// <summary>
    ///     This interface gives components behavior when they're held on the selected hand.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IHandSelected
    {
        [Obsolete("Use HandSelectedMessage instead")]
        void HandSelected(HandSelectedEventArgs eventArgs);
    }

    public class HandSelectedEventArgs : EventArgs
    {
        public HandSelectedEventArgs(EntityUid user)
        {
            User = user;
        }

        public EntityUid User { get; }
    }

    /// <summary>
    ///     Raised when an item entity held by a hand is selected.
    /// </summary>
    [PublicAPI]
    public class HandSelectedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that owns the selected hand.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Item in the hand that was selected.
        /// </summary>
        public EntityUid Item { get; }

        public HandSelectedEvent(EntityUid user, EntityUid item)
        {
            User = user;
            Item = item;
        }
    }
}
