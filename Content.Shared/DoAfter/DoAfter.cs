using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.DoAfter;

[Serializable, NetSerializable]
[DataDefinition]
[Access(typeof(SharedDoAfterSystem))]
public sealed partial class DoAfter
{
    [DataField("index", required:true)]
    public ushort Index;

    public DoAfterId Id => new(Args.User, Index);

    [IncludeDataField]
    public DoAfterArgs Args = default!;

    /// <summary>
    ///     Time at which this do after was started.
    /// </summary>
    [DataField("startTime", customTypeSerializer: typeof(TimeOffsetSerializer), required:true)]
    public TimeSpan StartTime;

    /// <summary>
    ///     The time at which this do after was canceled
    /// </summary>
    [DataField("cancelledTime", customTypeSerializer: typeof(TimeOffsetSerializer), required:true)]
    public TimeSpan? CancelledTime;

    /// <summary>
    ///     If true, this do after has finished, passed the final checks, and has raised its events.
    /// </summary>
    [DataField("completed")]
    public bool Completed;

    /// <summary>
    ///     Whether the do after has been canceled.
    /// </summary>
    public bool Cancelled => CancelledTime != null;

    /// <summary>
    ///     Position of the user relative to their parent when the do after was started.
    /// </summary>
    [NonSerialized]
    [DataField("userPosition")]
    public EntityCoordinates UserPosition;

    public NetCoordinates NetUserPosition;

    /// <summary>
    ///     Distance from the user to the target when the do after was started.
    /// </summary>
    [DataField("targetDistance")]
    public float TargetDistance;

    /// <summary>
    ///     If <see cref="DoAfterArgs.NeedHand"/> is true, this is the hand that was selected when the doafter started.
    /// </summary>
    [DataField("activeHand")]
    public string? InitialHand;

    /// <summary>
    ///     If <see cref="NeedHand"/> is true, this is the entity that was in the active hand when the doafter started.
    /// </summary>
    [NonSerialized]
    [DataField("activeItem")]
    public EntityUid? InitialItem;

    public NetEntity? NetInitialItem;

    // cached attempt event for the sake of avoiding unnecessary reflection every time this needs to be raised.
    [NonSerialized] public object? AttemptEvent;

    private DoAfter()
    {
    }

    public DoAfter(ushort index, DoAfterArgs args, TimeSpan startTime)
    {
        Index = index;

        Args = args;
        StartTime = startTime;
    }

    public DoAfter(IEntityManager entManager, DoAfter other)
    {
        Index = other.Index;
        Args = new(other.Args);
        StartTime = other.StartTime;
        CancelledTime = other.CancelledTime;
        Completed = other.Completed;
        UserPosition = other.UserPosition;
        TargetDistance = other.TargetDistance;
        InitialHand = other.InitialHand;
        InitialItem = other.InitialItem;

        NetUserPosition = other.NetUserPosition;
        NetInitialItem = other.NetInitialItem;
    }
}

/// <summary>
///     Simple struct that contains data required to uniquely identify a doAfter.
/// </summary>
/// <remarks>
///     Can be used to track currently active do-afters to prevent simultaneous do-afters.
/// </remarks>
public record struct DoAfterId(EntityUid Uid, ushort Index);
