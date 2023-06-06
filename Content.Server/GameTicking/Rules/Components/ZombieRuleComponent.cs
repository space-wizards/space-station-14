using Content.Shared.Zombies;

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
    ///   Settings for all players once the nerf player threshold is met
    ///     (Note, not all settings are applied to the given zombies)
    /// </summary>
    [DataField("nerfSettings"), ViewVariables(VVAccess.ReadWrite)]
    public ZombieSettings? NerfSettings = null;

    /// <summary>
    ///   Don't pick patient 0 for this long after rule start (probably since round start)
    /// </summary>
    [DataField("initialInfectDelaySecs"), ViewVariables(VVAccess.ReadWrite)]
    public float InitialInfectDelaySecs = 300;

    /// <summary>
    ///   How long between rulestart and announcing the zombie event (minimum)
    /// </summary>
    [DataField("announceMin"), ViewVariables(VVAccess.ReadWrite)]
    public float AnnounceMin = 660;

    /// <summary>
    ///   How long between rulestart and announcing the zombie event (maximum)
    /// </summary>
    [DataField("announceMax"), ViewVariables(VVAccess.ReadWrite)]
    public float AnnounceMax = 900;

    /// <summary>
    ///   How long the initial infected have to wait from roundstart before they are allowed to turn.
    /// </summary>
    [DataField("turnTimeMin"), ViewVariables(VVAccess.ReadWrite)]
    public float TurnTimeMin = 600;

    /// <summary>
    ///   The probability that the zombie round is going to get announced at all
    /// </summary>
    [DataField("announceChance"), ViewVariables(VVAccess.ReadWrite)]
    public float AnnounceChance = 0.8f;

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
    ///   The fraction of players at which we nerf all existing zombies if NerfSettings != null
    /// </summary>
    [DataField("nerfZombiesAt"), ViewVariables(VVAccess.ReadWrite)]
    public float NerfZombiesAt = 0.3f;

    /// <summary>
    ///   Ratio of infected players at which any existing initialInfected begin to turn.
    /// </summary>
    [DataField("forceZombiesAt"), ViewVariables(VVAccess.ReadWrite)]
    public float ForceZombiesAt = 0.3f;

    // -- Params below here are not really meant to be modified in YML
    // When we roll to announce the zombie event
    [DataField("announceAt"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AnnounceAt = TimeSpan.Zero;

    // When we infect the initial infected and tell them
    [DataField("infectInitialAt"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan InfectInitialAt = TimeSpan.Zero;

    // When Initial Infected can first turn
    [DataField("firstTurnAllowed"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan FirstTurnAllowed = TimeSpan.Zero;

    // If we have called a shuttle due to > 75% infection
    [DataField("calledShuttle75"), ViewVariables(VVAccess.ReadWrite)]
    public bool CalledShuttle75 = false;
    // If we have called a shuttle due to > 90% infection (in case 75% one cancelled)
    [DataField("calledShuttle90"), ViewVariables(VVAccess.ReadWrite)]
    public bool CalledShuttle90 = false;

    // If we have nerfed the zombies yet (applied NerfSettings if it is non-null)
    [DataField("nerfedZombies"), ViewVariables(VVAccess.ReadWrite)]
    public bool NerfedZombies = false;
    // If we have forced all initialInfected
    [DataField("forcedZombies"), ViewVariables(VVAccess.ReadWrite)]
    public bool ForcedZombies = false;
}
