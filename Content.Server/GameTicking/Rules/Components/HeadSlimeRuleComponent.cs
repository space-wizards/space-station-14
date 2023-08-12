using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(HeadSlimeRuleSystem))]
public sealed class HeadSlimeRuleComponent : Component
{
    public Dictionary<string, string> InitialHeadSlimeNames = new();

    [DataField("headSlimeQueenPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string HeadSlimeQueenPrototypeId = "InitialHeadSlimes";

    [DataField("initialInfectedNames")]
    public Dictionary<string, string> InitialInfectedNames = new();

    /// <summary>
    /// Whether or not the initial infected have been chosen.
    /// </summary>
    [DataField("infectedChosen")]
    public bool InfectedChosen;

    /// <summary>
    /// The amount of time between each check for the end of the round.
    /// </summary>
    [DataField("endCheckDelay")]
    public TimeSpan EndCheckDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The time at which the initial infected will be chosen.
    /// </summary>
    [DataField("startTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? StartTime;

    /// <summary>
    /// The minimum amount of time after the round starts that the initial infected will be chosen.
    /// </summary>
    [DataField("minStartDelay")]
    public TimeSpan MinStartDelay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The maximum amount of time after the round starts that the initial infected will be chosen.
    /// </summary>
    [DataField("maxStartDelay")]
    public TimeSpan MaxStartDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The sound that plays when someone becomes a head slime.
    /// </summary>
    [DataField("initialInfectedSound")]
    public SoundSpecifier InitialInfectedSound = new SoundPathSpecifier("/Audio/Ambience/Antag/headslime_start.ogg");

    /// <summary>
    /// How many players for each initial infected.
    /// </summary>
    [DataField("playersPerInfected")]
    public int PlayersPerInfected = 10;

    /// <summary>
    /// The maximum number of initial infected.
    /// </summary>
    [DataField("maxInitialInfected")]
    public int MaxInitialInfected = 6;

    /// <summary>
    /// After this amount of the crew become zombies, the shuttle will be automatically called.
    /// </summary>
    [DataField("headSlimeShuttleCallPercentage")]
    public float HeadSlimeShuttleCallPercentage = 0.5f;

    /// <summary>
    /// Have we called the evac shuttle yet?
    /// </summary>
    [DataField("shuttleCalled")]
    public bool ShuttleCalled;

    /// <summary>
    /// When the round will next check for round end.
    /// </summary>
    [DataField("nextRoundEndCheck", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextRoundEndCheck;

    
}
