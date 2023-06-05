using System.Runtime.Serialization;
using Content.Server.StationEvents.Metric;
using Robust.Shared.Serialization;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(GameDirectorSystem))]
public sealed class GameDirectorComponent : Component
{
    public const float MinimumTimeUntilFirstEvent = 300;

    /// <summary>
    /// How long until the next check for an event runs
    /// </summary>
    /// Default value is how long until first event is allowed
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeUntilNextEvent = MinimumTimeUntilFirstEvent;

    /// <summary>
    /// How long we've been in the current beat
    /// </summary>
    [DataField("beatTime"), ViewVariables(VVAccess.ReadWrite)]
    public float BeatTime = 0.0f;

    /// <summary>
    /// How long we've been in the current beat
    /// </summary>
    [DataField("currChaos"), ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics CurrChaos = new();

    /// <summary>
    /// The story we are currently executing from stories
    /// </summary>
    [DataField("currStoryName"), ViewVariables(VVAccess.ReadWrite)]
    public string CurrStoryName = "";

    /// <summary>
    /// Remaining beats in the story we are currently executing (a list of beat IDs)
    /// </summary>
    [DataField("currStory"), ViewVariables(VVAccess.ReadWrite)]
    public List<string> CurrStory = new List<string>();

    /// <summary>
    /// All possible story beats, by ID
    /// </summary>
    [DataField("storyBeats"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, StoryBeat> StoryBeats = new();

    /// <summary>
    /// A dictionary mapping story names to the list of beats for each story.
    /// One of these get picked randomly each time the current story is exausted.
    /// </summary>
    [DataField("stories"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, Story> Stories = new();

    /// <summary>
    /// A beat name we always use when we cannot find any stories to use.
    /// </summary>
    [DataField("fallbackBeatName"), ViewVariables(VVAccess.ReadWrite)]
    public string FallbackBeatName = "Peace";

    /// <summary>
    /// All the events that are allowed to run in the current story.
    /// </summary>
    [DataField("possibleEvents"), ViewVariables(VVAccess.ReadWrite)]
    public List<PossibleEvent> PossibleEvents = new();
    // Could have Chaos multipliers here, or multipliers per player (so stories are harder with more players).
}

/// <summary>
/// A series of named StoryBeats which we want to take the station through in the given sequence.
/// Gated by various settings such as the number of players
/// </summary>
[DataDefinition]
public sealed class Story
{
    /// <summary>
    /// A human-readable description string for logging / admins
    /// </summary>
    [DataField("description"), ViewVariables(VVAccess.ReadWrite)]
    public string Description = "<Story>";

    /// <summary>
    /// Minimum number of players on the station to pick this story
    /// </summary>
    [DataField("minPlayers"), ViewVariables(VVAccess.ReadWrite)]
    public int MinPlayers = -1;

    /// <summary>
    /// Maximum number of players on the station to pick this story
    /// </summary>
    [DataField("maxPlayers"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxPlayers = Int32.MaxValue;

    /// <summary>
    /// List of beat-ids in this story.
    /// </summary>
    [DataField("beats"), ViewVariables(VVAccess.ReadWrite)]
    public List<String> Beats = new();
}

/// <summary>
/// A point in the story of the station where the dynamic system tries to achieve a certian level of chaos
/// for instance you want a battle (goal has lots of hostiles)
/// then the next beat you might want a restoration of peace (goal has a balanced combat score)
/// then you might want to have the station heal up (goal has low medical, atmos and power scores)
///
/// In each case you create a beat and string them together into a story.
///
/// EndIfAnyWorse might be used for a battle to trigger when the chaos has become high enough.
/// endIfAllBetter is suitable for when you want the station to reach a given level of peace before you subject them to
/// the next round of chaos.
/// </summary>
[DataDefinition]
public sealed class StoryBeat
{
    /// <summary>
    /// A human-readable description string for logging / admins
    /// </summary>
    [DataField("description"), ViewVariables(VVAccess.ReadWrite)]
    public string Description = "<StoryBeat>";

    /// <summary>
    /// Which chaos levels we are driving in this beat and the values we are aiming for
    /// </summary>
    [DataField("goal"), ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics Goal = new ChaosMetrics();

    /// <summary>
    /// If the current metrics get worse than any of these, end the story beat
    /// For instance, too many hostiles or too little atmos
    /// </summary>
    [DataField("endIfAnyWorse"), ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics EndIfAnyWorse = new ChaosMetrics();

    /// <summary>
    /// If the current metrics get better than all of these, end the story beat
    /// For instance, medical, atmos, hostiles are all under control.
    /// </summary>
    [DataField("endIfAllBetter"), ViewVariables(VVAccess.ReadWrite)]
    public ChaosMetrics EndIfAllBetter = new ChaosMetrics();

    /// <summary>
    /// The number of seconds that we will remain in this state at minimum
    /// </summary>
    public float MinSecs = 480.0f;

    /// <summary>
    /// The number of seconds that we will remain in this state at maximum
    /// </summary>
    public float MaxSecs = 1200.0f;

    /// <summary>
    /// How many different events we choose from (at random) when performing this StoryBeat
    /// </summary>
    [DataField("randomEventLimit"), ViewVariables(VVAccess.ReadWrite)]
    public int RandomEventLimit = 3;
}

[DataDefinition]
public sealed class PossibleEvent
{
    public string PrototypeId = "";

    public ChaosMetrics Chaos = new();

    public PossibleEvent()
    {
    }

    public PossibleEvent(string prototypeId, ChaosMetrics chaos)
    {
        PrototypeId = prototypeId;
        Chaos = chaos;
    }
}
