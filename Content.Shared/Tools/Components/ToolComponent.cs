using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Components
{
    [RegisterComponent, NetworkedComponent] // TODO move tool system to shared, and make it a friend.
    public sealed partial class ToolComponent : Component
    {
        [DataField("qualities")]
        public PrototypeFlags<ToolQualityPrototype> Qualities { get; set; } = new();

        /// <summary>
        ///     For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speed")]
        public float SpeedModifier { get; set; } = 1;

        [DataField("useSound")]
        public SoundSpecifier? UseSound { get; set; }
    }

    /// <summary>
    /// Attempt event called *before* any do afters to see if the tool usage should succeed or not.
    /// Raised on both the tool and then target.
    /// </summary>
    public sealed class ToolUseAttemptEvent : CancellableEntityEventArgs
    {
        public EntityUid User { get; }

        public ToolUseAttemptEvent(EntityUid user)
        {
            User = user;
        }
    }

    /// <summary>
    /// Event raised on the user of a tool to see if they can actually use it.
    /// </summary>
    [ByRefEvent]
    public struct ToolUserAttemptUseEvent
    {
        public EntityUid? Target;
        public bool Cancelled = false;

        public ToolUserAttemptUseEvent(EntityUid? target)
        {
            Target = target;
        }
    }
}
