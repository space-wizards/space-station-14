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
    ///   Don't pick patient 0 for this long after rule start (probably since round start)
    /// </summary>
    [DataField("initialInfectDelaySecs"), ViewVariables(VVAccess.ReadWrite)]
    public float InitialInfectDelaySecs = 300;

    /// <summary>
    ///   How long between rulestart and announcing the zombie event (minimum)
    /// </summary>
    [DataField("announceMinimum"), ViewVariables(VVAccess.ReadWrite)]
    public float AnnounceMinimum = 480;

    /// <summary>
    ///   How long between rulestart and announcing the zombie event (maximum)
    /// </summary>
    [DataField("announceMaximum"), ViewVariables(VVAccess.ReadWrite)]
    public float AnnounceMaximum = 720;

    /// <summary>
    ///   How long the initial infected have to wait from roundstart before they are allowed to turn.
    /// </summary>
    [DataField("announceMaximum"), ViewVariables(VVAccess.ReadWrite)]
    public float TurnTimeMinimum = 600;

    /// <summary>
    ///   The probability that the zombie round is going to get announced at all
    /// </summary>
    [DataField("announceChance"), ViewVariables(VVAccess.ReadWrite)]
    public float AnnounceChance = 0.8f;

    public TimeSpan AnnounceAt = TimeSpan.Zero;
    public TimeSpan InfectInitialAt = TimeSpan.Zero;
    public TimeSpan FirstTurnAllowed = TimeSpan.Zero;

}
