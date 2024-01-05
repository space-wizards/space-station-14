using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ZombieRuleSystem))]
public sealed partial class ZombieRuleComponent : Component
{
    [DataField("initialInfectedNames")]
    public Dictionary<string, string> InitialInfectedNames = new();

    [DataField("patientZeroPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string PatientZeroPrototypeId = "InitialInfected";

    /// <summary>
    /// The amount of time between each check for the end of the round.
    /// </summary>
    [DataField("endCheckDelay")]
    public TimeSpan EndCheckDelay = TimeSpan.FromSeconds(30);

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
    /// The sound that plays when someone becomes an initial infected.
    /// todo: this should have a unique sound instead of reusing the zombie one.
    /// </summary>
    [DataField("initialInfectedSound")]
    public SoundSpecifier InitialInfectedSound = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");

    /// <summary>
    /// The minimum amount of time initial infected have before they start taking infection damage.
    /// </summary>
    [DataField("minInitialInfectedGrace")]
    public TimeSpan MinInitialInfectedGrace = TimeSpan.FromMinutes(12.5f);

    /// <summary>
    /// The maximum amount of time initial infected have before they start taking damage.
    /// </summary>
    [DataField("maxInitialInfectedGrace")]
    public TimeSpan MaxInitialInfectedGrace = TimeSpan.FromMinutes(15f);

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

    [ValidatePrototypeId<EntityPrototype>]
    public const string ZombifySelfActionPrototype = "ActionTurnUndead";
}
