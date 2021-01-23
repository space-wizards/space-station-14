using System;
using Robust.Shared.Analyzers;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects
{
    /// <summary>
    ///     Implements behavior when an entity is disarmed.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IDisarmedAct
    {
        /// <summary>
        ///     Behavior when the entity is disarmed.
        ///     Return true to prevent the default disarm behavior,
        ///     or rest of IDisarmedAct behaviors that come after this one from happening.
        /// </summary>
        bool Disarmed(DisarmedActEventArgs eventArgs);

        /// <summary>
        ///     Priority for this disarm act.
        ///     Used to determine act execution order.
        /// </summary>
        int Priority => 0;
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

        /// <summary>
        ///     Probability for push/knockdown.
        /// </summary>
        public float PushProbability { get; init; }
    }
}
