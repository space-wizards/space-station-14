using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Defines basic data for a station event
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class StationEventComponent : Component
{
    public const float WeightVeryLow = 0.0f;
    public const float WeightLow = 5.0f;
    public const float WeightNormal = 10.0f;
    public const float WeightHigh = 15.0f;
    public const float WeightVeryHigh = 20.0f;

    [DataField("weight")]
    public float Weight = WeightNormal;

    [DataField("startAnnouncement")]
    public string? StartAnnouncement;

    [DataField("endAnnouncement")]
    public string? EndAnnouncement;

    [DataField("startAudio")]
    public SoundSpecifier? StartAudio;

    [DataField("endAudio")]
    public SoundSpecifier? EndAudio;

    /// <summary>
    ///     In minutes, when is the first round time this event can start
    /// </summary>
    [DataField("earliestStart")]
    public int EarliestStart = 5;

    /// <summary>
    ///     In minutes, the amount of time before the same event can occur again
    /// </summary>
    [DataField("reoccurrenceDelay")]
    public int ReoccurrenceDelay = 30;

    /// <summary>
    ///     How long after being added does the event start
    /// </summary>
    [DataField("startDelay")]
    public TimeSpan StartDelay = TimeSpan.Zero;

    /// <summary>
    ///     How long the event lasts.
    /// </summary>
    [DataField("duration")]
    public TimeSpan? Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     The max amount of time the event lasts.
    /// </summary>
    [DataField("maxDuration")]
    public TimeSpan? MaxDuration;

    /// <summary>
    ///     How many players need to be present on station for the event to run
    /// </summary>
    /// <remarks>
    ///     To avoid running deadly events with low-pop
    /// </remarks>
    [DataField("minimumPlayers")]
    public int MinimumPlayers;

    /// <summary>
    ///     How many times this even can occur in a single round
    /// </summary>
    [DataField("maxOccurrences")]
    public int? MaxOccurrences;

    /// <summary>
    /// When the station event starts.
    /// </summary>
    [DataField("startTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan StartTime;

    /// <summary>
    /// When the station event ends.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? EndTime;
}
