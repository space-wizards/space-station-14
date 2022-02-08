using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Act
{
    /// <summary>
    ///     Implements behavior when an entity is disarmed.
    /// </summary>
    [RequiresExplicitImplementation, Obsolete("Use the directed event instead.")]
    public interface IDisarmedAct
    {
        /// <summary>
        ///     Behavior when the entity is disarmed.
        ///     Return true to prevent the default disarm behavior,
        ///     or rest of IDisarmedAct behaviors that come after this one from happening.
        /// </summary>
        bool Disarmed(DisarmedActEvent @event);

        /// <summary>
        ///     Priority for this disarm act.
        ///     Used to determine act execution order.
        /// </summary>
        int Priority => 0;
    }

    public class DisarmedActEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     The entity being disarmed.
        /// </summary>
        public EntityUid Target { get; init; }

        /// <summary>
        ///     The entity performing the disarm.
        /// </summary>
        public EntityUid Source { get; init; }

        /// <summary>
        ///     Probability for push/knockdown.
        /// </summary>
        public float PushProbability { get; init; }
    }
}
