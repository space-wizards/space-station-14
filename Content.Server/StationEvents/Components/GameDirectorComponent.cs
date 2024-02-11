using System.Runtime.Serialization;
using Content.Server.StationEvents.Metric;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(GameDirectorSystem))]
public sealed partial class GameDirectorComponent : Component
{
    public const float MinimumTimeUntilFirstEvent = 300; // in seconds

    /// <summary>
    ///   How long until the next check for an event runs
    ///   Default value is how long until first event is allowed
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeNextEvent;

    /// <summary>
    ///   When the current beat started
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BeatStart;

    /// <summary>
    ///   The chaos we measured last time we ran
    ///   This is helpful for ViewVariables and perhaps as a cache to hold chaos for other functions to use.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ChaosMetrics CurrentChaos = new();

    /// <summary>
    ///   The story we are currently executing from stories (for easier debugging). Since it is for
    ///   debugging, it does not need a DataField.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<StoryPrototype> CurrentStoryName;

    /// <summary>
    ///   Remaining beats in the story we are currently executing (a list of beat IDs)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<StoryBeatPrototype>> RemainingBeats = new();

    /// <summary>
    ///   Which stories the director can choose from (so we can change flavor of director by loading different stories)
    ///   One of these get picked randomly each time the current story is exhausted.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StoryPrototype>[]? Stories;

    /// <summary>
    ///   A beat name we always use when we cannot find any stories to use.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StoryBeatPrototype> FallbackBeatName = "Peace";

    /// <summary>
    ///   All the events that are allowed to run in the current story.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<PossibleEvent> PossibleEvents = new();
    // Could have Chaos multipliers here, or multipliers per player (so stories are harder with more players).
}

/// <summary>
///   A series of named StoryBeats which we want to take the station through in the given sequence.
///   Gated by various settings such as the number of players
/// </summary>
[DataDefinition]
[Prototype("story")]
public sealed partial class StoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///   A human-readable description string for logging / admins
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string Description;

    /// <summary>
    ///   Minimum number of players on the station to pick this story
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int MinPlayers = -1;

    /// <summary>
    ///   Maximum number of players on the station to pick this story
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxPlayers = Int32.MaxValue;

    /// <summary>
    ///   List of beat-ids in this story.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StoryBeatPrototype>[]? Beats;
}

/// <summary>
///   A point in the story of the station where the dynamic system tries to achieve a certain level of chaos
///   for instance you want a battle (goal has lots of hostiles)
///   then the next beat you might want a restoration of peace (goal has a balanced combat score)
///   then you might want to have the station heal up (goal has low medical, atmos and power scores)
///
///   In each case you create a beat and string them together into a story.
///
///   EndIfAnyWorse might be used for a battle to trigger when the chaos has become high enough.
///   endIfAllBetter is suitable for when you want the station to reach a given level of peace before you subject them to
///   the next round of chaos.
/// </summary>
[DataDefinition]
[Prototype("storyBeat")]
public sealed partial class StoryBeatPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///   A human-readable description string for logging / admins
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Description;

    /// <summary>
    ///   Which chaos levels we are driving in this beat and the values we are aiming for
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics Goal = new ChaosMetrics();

    /// <summary>
    ///   Early end if things deteriorate too much
    ///
    ///   If the current metrics get worse than any of these, end the story beat
    ///   For instance, too many hostiles or too little atmos
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics EndIfAnyWorse = new ChaosMetrics();

    /// <summary>
    ///   Early end if life is good enough
    ///
    ///   If the current metrics get better than all of these, end the story beat
    ///   For instance, medical, atmos, hostiles are all under control.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics EndIfAllBetter = new ChaosMetrics();

    /// <summary>
    ///   The number of seconds that we will remain in this state at minimum
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinSecs = 480.0f;

    /// <summary>
    ///   The number of seconds that we will remain in this state at maximum
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxSecs = 1200.0f;

    /// <summary>
    ///   Seconds between events during this beat (min)
    ///   2 minute default (120)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EventDelayMin = 120.0f;

    /// <summary>
    ///   Seconds between events during this beat (min)
    ///   6 minute default (360)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EventDelayMax = 360.0f;

    /// <summary>
    ///   How many different events we choose from (at random) when performing this StoryBeat
    /// </summary>
    ///
    /// The director is making a priority pick. But to ensure it doesn't ALWAYS pick the very best we actually
    ///  pick randomly from the top few events (RandomEventLimit).
    /// By tuning RandomEventLimit you can decide on a per beat basis how much the director is "directing" and
    ///  how much it's acting like a random system. Some randomness is often good to spice things up.
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int RandomEventLimit = 3;
}

/// <summary>
///   Caches a possible StationEvent prototype with the chaos expected (from the game rule's data)
///   A list of PossibleEvents are built and cached by the game director.
/// </summary>
[DataDefinition]
public sealed partial class PossibleEvent
{
    /// <summary>
    ///   ID of a station event prototype (anomaly, spiders, pizzas, etc) that could occur
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId StationEvent;

    /// <summary>
    ///   Expected Chaos changes when this event occurs.
    ///   Used by the GameDirector, which picks an event expected to make the desired chaos changes.
    ///   Copy of the StationEventComponent.Chaos field from the relevant event.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics Chaos = new();

    public PossibleEvent()
    {
    }

    public PossibleEvent(EntProtoId stationEvent, ChaosMetrics chaos)
    {
        StationEvent = stationEvent;
        Chaos = chaos;
    }
}
