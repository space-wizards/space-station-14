using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Devices
{
    /// <summary>
    /// This represents the actual timer game object, not the abstract idea of a timer.
    /// </summary>
    [RegisterComponent]
    public class SharedIoTimerComponent : Component
    {
        public override string Name => "IoTimer";

        /// <summary>
        /// Max duration in seconds.
        /// </summary>
        public const int MaxDuration = 1000;

        /// <summary>
        /// Min duration in seconds.
        /// </summary>
        public const int MinDuration = 1;

        /// <summary>
        /// Whether or not the timer is active.
        /// If false, the timer is paused.
        /// If true, the timer is active.
        /// </summary>
        public bool IsActive = false;

        public bool IsPaused = false;

        /// <summary>
        /// This is set when the timer is paused.
        /// </summary>
        public TimeSpan PausedTime = TimeSpan.Zero;

        public (TimeSpan, TimeSpan) StartAndEndTimes = (TimeSpan.Zero, TimeSpan.Zero);

        public const int DefaultDuration = 5;

        [DataField("duration")]
        public int Duration = DefaultDuration;

        public const bool DefaultShouldPlaySound = false;

        [DataField("shouldPlaySound")]
        public bool ShouldPlaySound = DefaultShouldPlaySound;
    }

    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum IoTimerUiKey
    {
        Key,
    }

    /// <summary>
    /// Represents a <see cref="SharedIoTimerComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public class IoTimerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public int Duration { get; }
        public (TimeSpan, TimeSpan) StartAndEndTimes { get; }
        public bool IsPaused { get; }

        public bool IsActive { get; }

        public IoTimerBoundUserInterfaceState(int duration, (TimeSpan, TimeSpan) startAndEndTimes, bool isActive, bool isPaused)
        {
            Duration = duration;
            StartAndEndTimes = startAndEndTimes;
            IsActive = isActive;
            IsPaused = isPaused;
        }
    }

    [Serializable, NetSerializable]
    public class IoTimerSendToggleMessage : BoundUserInterfaceMessage
    {
        public IoTimerSendToggleMessage() {}
    }

    [Serializable, NetSerializable]
    public class IoTimerSendResetMessage : BoundUserInterfaceMessage
    {
        public IoTimerSendResetMessage() {}
    }

    [Serializable, NetSerializable]
    public class IoTimerSendTogglePauseMessage : BoundUserInterfaceMessage
    {
        public IoTimerSendTogglePauseMessage() {}
    }

    [Serializable, NetSerializable]
    public class IoTimerUpdateDurationMessage : BoundUserInterfaceMessage
    {
        public int Duration { get; }

        public IoTimerUpdateDurationMessage( int duration )
        {
            Duration = duration;
        }
    }
}
