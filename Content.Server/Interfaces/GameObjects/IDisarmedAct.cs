using System;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects
{
    /// <summary>
    ///     Implements behavior when an entity is disarmed.
    /// </summary>
    public interface IDisarmedAct
    {
        /// <summary>
        ///     Behavior when the entity is disarmed.
        ///     Return true to prevent the default disarm behavior,
        ///     or rest of IDisarmAct behaviors that come after this one from happening.
        /// </summary>
        bool Disarmed(DisarmedActEventArgs eventArgs);
    }

    public class DisarmedActEventArgs : EventArgs
    {
        /// <summary>
        ///     The entity being disarmed.
        /// </summary>
        public IEntity Target { get; init; }

        /// <summary>
        ///     The entity performing the disarm.
        /// </summary>
        public IEntity Source { get; init; }
    }
}
