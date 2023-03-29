using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Components
{
    [RegisterComponent, NetworkedComponent] // TODO move tool system to shared, and make it a friend.
    public sealed class ToolComponent : Component
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
    ///     Attempt event called *before* any do afters to see if the tool usage should succeed or not.
    ///     You can change the fuel consumption by changing the Fuel property.
    /// </summary>
    public sealed class ToolUseAttemptEvent : CancellableEntityEventArgs
    {
        public float Fuel { get; set; }
        public EntityUid User { get; }

        public ToolUseAttemptEvent(float fuel, EntityUid user)
        {
            Fuel = fuel;
            User = user;
        }
    }

    /// <summary>
    /// Event raised on the user of a tool to see if they can actually use it.
    /// </summary>
    [ByRefEvent]
    public struct ToolUserAttemptUseEvent
    {
        public EntityUid User;
        public EntityUid? Target;
        public bool Cancelled = false;

        public ToolUserAttemptUseEvent(EntityUid user, EntityUid? target)
        {
            User = user;
            Target = target;
        }
    }

    /// <summary>
    ///     Attempt event called *after* any do afters to see if the tool usage should succeed or not.
    ///     You can use this event to consume any fuel needed.
    /// </summary>
    public sealed class ToolUseFinishAttemptEvent : CancellableEntityEventArgs
    {
        public float Fuel { get; }
        public EntityUid User { get; }

        public ToolUseFinishAttemptEvent(float fuel, EntityUid user)
        {
            Fuel = fuel;
        }
    }

    public sealed class ToolEventData
    {
        public readonly Object? Ev;
        public readonly Object? CancelledEv;
        public readonly float Fuel;
        public readonly EntityUid? TargetEntity;

        public ToolEventData(Object? ev, float fuel = 0f, Object? cancelledEv = null, EntityUid? targetEntity = null)
        {
            Ev = ev;
            CancelledEv = cancelledEv;
            Fuel = fuel;
            TargetEntity = targetEntity;
        }
    }
}
