using Content.Shared.Zombies;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;


[RegisterComponent, Access(typeof(ZombieRuleSystem))]
public sealed class ZombieRuleComponent : Component
{
    public Dictionary<string, string> InitialInfectedNames = new();

    public string PatientZeroPrototypeID = "InitialInfected";
    public const string ZombifySelfActionPrototype = "TurnUndead";

    /// <summary>
    ///   After this many seconds the players will be forced to turn into zombies (at minimum)
    ///   Defaults to 10 minutes. 10*60 = 600 seconds.
    ///
    ///   Zombie time for a given player is:
    ///   random MinZombieForceSecs to MaxZombieForceSecs + up to PlayerZombieForceVariation
    /// </summary>
    [DataField("minZombieForceSecs"), ViewVariables(VVAccess.ReadWrite)]
    public float MinZombieForceSecs = 600;

    /// <summary>
    ///   After this many seconds the players will be forced to turn into zombies (at maximum)
    ///   Defaults to 15 minutes. 15*60 = 900 seconds.
    /// </summary>
    [DataField("maxZombieForceSecs"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxZombieForceSecs = 900;

    /// <summary>
    ///   How many additional seconds each player will get (at random) to scatter forced zombies over time.
    ///   Defaults to 2 minutes. 2*60 = 120 seconds.
    /// </summary>
    [DataField("playerZombieForceVariationSecs"), ViewVariables(VVAccess.ReadWrite)]
    public float PlayerZombieForceVariationSecs = 120;

    /// <summary>
    ///   Settings for the first round of zombies (so called patient 0)
    /// </summary>
    [DataField("earlySettings"), ViewVariables(VVAccess.ReadWrite)]
    public ZombieSettings EarlySettings = default!;

    /// <summary>
    ///   Settings for the later rounds of zombies (victims of the first). Probably weaker.
    /// </summary>
    [DataField("victimSettings"), ViewVariables(VVAccess.ReadWrite)]
    public ZombieSettings VictimSettings = default!;

    /// <summary>
    ///   Don't pick patient 0 for this long after rule start (probably since round start)
    /// </summary>
    [DataField("initialInfectDelaySecs"), ViewVariables(VVAccess.ReadWrite)]
    public float InitialInfectDelaySecs = 300;

    /// <summary>
    ///   How long the initial infected have to wait from roundstart before they are allowed to turn.
    /// </summary>
    [DataField("turnTimeMin"), ViewVariables(VVAccess.ReadWrite)]
    public float TurnTimeMin = 600;

    /// <summary>
    ///   If more than this fraction of the crew get wiped by zombies, but then zombies die... end the round (win)
    /// </summary>
    [DataField("winEndsRoundAbove"), ViewVariables(VVAccess.ReadWrite)]
    public float WinEndsRoundAbove = 0.6f;

    /// <summary>
    ///   The minimum number of players per each one that gets infected.
    /// Will use Max( cvar zombie.players_per_infected, <this_value> )
    /// </summary>
    [DataField("playersPerInfected"), ViewVariables(VVAccess.ReadWrite)]
    public int PlayersPerInfected = 10;

    /// <summary>
    ///   The maximum infected players overall.
    /// Will use Min( cvar zombie.max_initial_infected, <this_value> )
    /// </summary>
    [DataField("maxInitialInfected"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxInitialInfected = 6;

    /// <summary>
    ///   Ratio of infected players at which any existing initialInfected begin to turn.
    /// </summary>
    [DataField("forceZombiesAt"), ViewVariables(VVAccess.ReadWrite)]
    public float ForceZombiesAt = 0.5f;

    /// <summary>
    ///   Remaining shuttle calls (each is a ratio of infection)
    /// </summary>
    [DataField("shuttleCalls"), ViewVariables(VVAccess.ReadWrite)]
    public List<float> ShuttleCalls = new(){0.75f, 0.90f};

    // -- Params below here are not really meant to be modified in YML
    // When we infect the initial infected and tell them
    [DataField("infectInitialAt", customTypeSerializer:typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? InfectInitialAt;

    // When Initial Infected can first turn
    [DataField("firstTurnAllowed", customTypeSerializer:typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? FirstTurnAllowed;

    // If we have forced all initialInfected
    [DataField("forcedZombies"), ViewVariables(VVAccess.ReadWrite)]
    public bool ForcedZombies = false;
}
