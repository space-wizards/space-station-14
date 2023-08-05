using Content.Shared.Roles;
using Content.Shared.Zombies;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ZombieRuleSystem))]
public sealed class ZombieRuleComponent : Component
{
    [DataField("initialInfectedNames")]
    public Dictionary<string, string> InitialInfectedNames = new();

    [DataField("patientZeroPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string PatientZeroPrototypeId = "InitialInfected";

    /// <summary>
    /// The minimum amount of time after the round starts that the initial infected will be chosen.
    /// </summary>
    [DataField("minStartDelay")]
    public TimeSpan MinStartDelay = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The maximum amount of time after the round starts that the initial infected will be chosen.
    /// </summary>
    [DataField("maxStartDelay")]
    public TimeSpan MaxStartDelay = TimeSpan.FromMinutes(10);

    /// <summary>
    ///   How long the initial infected have to wait from initial infected selection to before they are allowed to turn.
    /// </summary>
    [DataField("turnTimeMin"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TurnTimeMin = TimeSpan.FromMinutes(5);

    /// <summary>
    ///   Settings for the first round of zombies (so called patient 0)
    /// </summary>
    [DataField("earlySettings"), ViewVariables(VVAccess.ReadWrite)]
    public ZombieSettings EarlySettings = default!;

    /// <summary>
    ///   Settings for the later rounds of zombies (victims of the first). Probably weaker.
    /// </summary>
    [DataField("victimSettings", required: false), ViewVariables(VVAccess.ReadWrite)]
    public ZombieSettings? VictimSettings;

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
    /// The sound that plays when someone becomes an initial infected.
    /// todo: this should have a unique sound instead of reusing the zombie one.
    /// </summary>
    [DataField("initialInfectedSound")]
    public SoundSpecifier InitialInfectedSound = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");

    /// <summary>
    ///   If more than this fraction of the crew get wiped by zombies, but then zombies die... end the round (win)
    /// </summary>
    [DataField("winEndsRoundAbove"), ViewVariables(VVAccess.ReadWrite)]
    public float WinEndsRoundAbove = 0.6f;

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
    [DataField("zombieShuttleCallPercentage")]
    public float ZombieShuttleCallPercentage = 0.5f;

    // -- Params below here are not really meant to be modified in YML
    // When we infect the initial infected and tell them
    [DataField("infectInitialAt", customTypeSerializer:typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? InfectInitialAt;

    [DataField("shuttleCalled"), ViewVariables(VVAccess.ReadWrite)]
    public bool ShuttleCalled;

    // When Initial Infected can first turn
    [DataField("firstTurnAllowed", customTypeSerializer:typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? FirstTurnAllowed;

    public const string ZombifySelfActionPrototype = "TurnUndead";
}
