using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable;

/// <summary>
/// This contains all the events raised by the SharedStunSystem
/// </summary>
public abstract partial class SharedStunSystem
{
    /// <summary>
    ///     Raised directed on an entity when it is stunned.
    /// </summary>
    [ByRefEvent]
    public record struct StunnedEvent;

    /// <summary>
    ///     Raised directed on an entity when it is knocked down.
    /// </summary>
    [ByRefEvent]
    public record struct KnockDownAttemptEvent(bool Cancelled = false)
    {
        public bool AutoStand;
    }

    /// <summary>
    ///     Raised directed on an entity when it is knocked down.
    /// </summary>
    [ByRefEvent]
    public record struct KnockedDownEvent
    {
        public TimeSpan KnockdownTime;
    }

    /// <summary>
    ///     Raised on an entity that needs to refresh its knockdown modifiers
    /// </summary>
    [ByRefEvent]
    public record struct KnockedDownRefreshEvent
    {
        public float SpeedModifier;
        public float FrictionModifier;
    }

    /// <summary>
    ///     Raised directed on an entity when it tries to stand up
    /// </summary>
    [ByRefEvent]
    public record struct StandUpAttemptEvent(bool Cancelled);

    [ByRefEvent]
    public record struct StandUpArgsEvent
    {
        public bool AutoStand;
        public TimeSpan DoAfterTime;
    }

    [ByRefEvent]

    public record struct KnockdownEndEvent;

    /// <summary>
    ///     Raised when you click on the Knocked Down Alert
    /// </summary>
    public sealed partial class KnockedDownAlertEvent : BaseAlertEvent;

    [ByRefEvent, Serializable, NetSerializable]
    public sealed partial class TryStandDoAfterEvent : SimpleDoAfterEvent;

    [Serializable, NetSerializable]
    public sealed class ForceStandUpEvent : EntityEventArgs;
}
