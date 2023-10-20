using System.Runtime.Serialization;
using Content.Server.StationEvents.Metric;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(GameDirectorSystem))]
public sealed partial class GameDirectorComponent : Component
{
    public const float MinimumTimeUntilFirstEvent = 300;

    /// <summary>
    ///   How long until the next check for an event runs
    /// </summary>
    ///   Default value is how long until first event is allowed
    [DataField("timeNextEvent", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeNextEvent;

    /// <summary>
    ///   When the current beat started
    /// </summary>
    [DataField("beatStart", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BeatStart;

    /// <summary>
    ///   The chaos we measured last time we ran
    ///   This is helpful for ViewVariables and perhaps as a cache to hold chaos for other functions to use.
    /// </summary>
    [DataField("currentChaos"), ViewVariables(VVAccess.ReadOnly)]
    public ChaosMetrics CurrentChaos = new();

    /// <summary>
    ///   The story we are currently executing from stories
    /// </summary>
    [DataField("currentStoryName"), ViewVariables(VVAccess.ReadOnly)]
    public string CurrentStoryName = "";

    /// <summary>
    ///   Remaining beats in the story we are currently executing (a list of beat IDs)
    /// </summary>
    // Stack is not compatible with DataField [DataField("currStory"), ViewVariables(VVAccess.ReadWrite)]
    public Stack<string> CurrStory = new();

    /// <summary>
    ///   All possible story beats, by ID
    /// </summary>
    [DataField("storyBeats"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, StoryBeat> StoryBeats = new();

    /// <summary>
    ///   A dictionary mapping story names to the list of beats for each story.
    ///   One of these get picked randomly each time the current story is exhausted.
    /// </summary>
    [DataField("stories"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, Story> Stories = new();

    /// <summary>
    ///   A beat name we always use when we cannot find any stories to use.
    /// </summary>
    [DataField("fallbackBeatName"), ViewVariables(VVAccess.ReadWrite)]
    public string FallbackBeatName = "Peace";

    /// <summary>
    ///   All the events that are allowed to run in the current story.
    /// </summary>
    [DataField("possibleEvents"), ViewVariables(VVAccess.ReadWrite)]
    public List<PossibleEvent> PossibleEvents = new();
    // Could have Chaos multipliers here, or multipliers per player (so stories are harder with more players).
}

/// <summary>
///   A series of named StoryBeats which we want to take the station through in the given sequence.
///   Gated by various settings such as the number of players
/// </summary>
[DataDefinition]
public sealed partial class Story
{
    /// <summary>
    ///   A human-readable description string for logging / admins
    /// </summary>
    [DataField("description"), ViewVariables(VVAccess.ReadWrite)]
    public string Description = "<Story>";

    /// <summary>
    ///   Minimum number of players on the station to pick this story
    /// </summary>
    [DataField("minPlayers"), ViewVariables(VVAccess.ReadWrite)]
    public int MinPlayers = -1;

    /// <summary>
    ///   Maximum number of players on the station to pick this story
    /// </summary>
    [DataField("maxPlayers"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxPlayers = Int32.MaxValue;

    /// <summary>
    ///   List of beat-ids in this story.
    /// </summary>
    [DataField("beats"), ViewVariables(VVAccess.ReadWrite)]
    public List<String> Beats = new();
}

/// <summary>
///   A point in the story of the station where the dynamic system tries to achieve a certian level of chaos
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
public sealed partial class StoryBeat
{
    /// <summary>
    ///   A human-readable description string for logging / admins
    /// </summary>
    [DataField("description"), ViewVariables(VVAccess.ReadWrite)]
    public string Description = "<StoryBeat>";

    /// <summary>
    ///   Which chaos levels we are driving in this beat and the values we are aiming for
    /// </summary>
    [DataField("goal"), ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics Goal = new ChaosMetrics();

    /// <summary>
    ///   If the current metrics get worse than any of these, end the story beat
    ///   For instance, too many hostiles or too little atmos
    /// </summary>
    [DataField("endIfAnyWorse"), ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics EndIfAnyWorse = new ChaosMetrics();

    /// <summary>
    ///   If the current metrics get better than all of these, end the story beat
    ///   For instance, medical, atmos, hostiles are all under control.
    /// </summary>
    [DataField("endIfAllBetter"), ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics EndIfAllBetter = new ChaosMetrics();

    /// <summary>
    ///   The number of seconds that we will remain in this state at minimum
    /// </summary>
    [DataField("minSecs"), ViewVariables(VVAccess.ReadWrite)]
    public float MinSecs = 480.0f;

    /// <summary>
    ///   The number of seconds that we will remain in this state at maximum
    /// </summary>
    [DataField("maxSecs"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxSecs = 1200.0f;

    /// <summary>
    ///   Seconds between events during this beat (min)
    ///   2 minute default (120)
    /// </summary>
    [DataField("eventDelayMin"), ViewVariables(VVAccess.ReadWrite)]
    public float EventDelayMin = 120.0f;

    /// <summary>
    ///   Seconds between events during this beat (min)
    ///   6 minute default (360)
    /// </summary>
    [DataField("eventDelayMax"), ViewVariables(VVAccess.ReadWrite)]
    public float EventDelayMax = 360.0f;

    /// <summary>
    ///   How many different events we choose from (at random) when performing this StoryBeat
    /// </summary>
    [DataField("randomEventLimit"), ViewVariables(VVAccess.ReadWrite)]
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
    [DataField("stationEvent", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string StationEvent = "";

    /// <summary>
    ///   Expected Chaos changes when this event occurs.
    ///   Used by the GameDirector, which picks an event expected to make the desired chaos changes.
    ///   Copy of the StationEventComponent.Chaos field from the relevant event.
    /// </summary>
    [DataField("chaos"), ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics Chaos = new();

    public PossibleEvent()
    {
    }

    public PossibleEvent(string stationEvent, ChaosMetrics chaos)
    {
        StationEvent = stationEvent;
        Chaos = chaos;
    }
}
