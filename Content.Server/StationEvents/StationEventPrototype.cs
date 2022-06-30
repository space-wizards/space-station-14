using Content.Shared.Sound;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents;

[Prototype("stationEvent")]
public sealed class StationEventPrototype : IPrototype
{
    public const float WeightVeryLow = 0.0f;
    public const float WeightLow = 5.0f;
    public const float WeightNormal = 10.0f;
    public const float WeightHigh = 15.0f;
    public const float WeightVeryHigh = 20.0f;

    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name", required: true)]
    public string Name = default!;

    [DataField("weight")]
    public float Weight = WeightNormal;

    [DataField("startAnnouncement")]
    public string? StartAnnouncement = null;

    [DataField("endAnnouncement")]
    public string? EndAnnouncement = null;

    [DataField("startAudio")]
    public SoundSpecifier? StartAudio = new SoundPathSpecifier("/Audio/Announcements/attention.ogg");

    [DataField("endAudio")]
    public SoundSpecifier? EndAudio = null;

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
    ///     When in the lifetime to call Start().
    /// </summary>
    [DataField("startAfter")]
    public float StartAfter = 0.0f;

    /// <summary>
    ///     When in the lifetime the event should end.
    /// </summary>
    [DataField("endAfter")]
    public float EndAfter = 0.0f;

    /// <summary>
    ///     How many players need to be present on station for the event to run
    /// </summary>
    /// <remarks>
    ///     To avoid running deadly events with low-pop
    /// </remarks>
    [DataField("minimumPlayers")]
    public int MinimumPlayers = 0;

    /// <summary>
    ///     How many times this even can occur in a single round
    /// </summary>
    [DataField("maxOccurrences")]
    public int? MaxOccurrences = null;

    /// <summary>
    ///     Whether or not the event is announced when it is run
    /// </summary>
    [DataField("announceEvent")]
    public bool AnnounceEvent = true;
}
