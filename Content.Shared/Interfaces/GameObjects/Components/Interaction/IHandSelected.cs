#nullable enable
using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
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
        public HandSelectedEventArgs(IEntity user)
        {
            User = user;
        }

        public IEntity User { get; }
    }

    /// <summary>
    ///     Raised when an entity item in a hand is selected.
    /// </summary>
    [PublicAPI]
    public class HandSelectedMessage : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that owns the selected hand.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The item in question.
        /// </summary>
        public IEntity Item { get; }

        public HandSelectedMessage(IEntity user, IEntity item)
        {
            User = user;
            Item = item;
        }
    }
}
