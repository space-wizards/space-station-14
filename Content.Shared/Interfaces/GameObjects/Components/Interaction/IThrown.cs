#nullable enable
using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when thrown.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IThrown
    {
        void Thrown(ThrownEventArgs eventArgs);
    }

    public class ThrownEventArgs : EventArgs
    {
        public ThrownEventArgs(IEntity user)
        {
            User = user;
        }

        public IEntity User { get; }
    }

    /// <summary>
    ///     Raised when throwing the entity in your hands.
    /// </summary>
    [PublicAPI]
    public class ThrownMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that threw the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was thrown.
        /// </summary>
        public IEntity Thrown { get; }

        public ThrownMessage(IEntity user, IEntity thrown)
        {
            User = user;
            Thrown = thrown;
        }
    }
}
