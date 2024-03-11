using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ZombieRuleSystem))]
public sealed partial class ZombieRuleComponent : Component
{
    [DataField]
    public Dictionary<string, string> InitialInfectedNames = new();

    [DataField]
    public ProtoId<AntagPrototype> PatientZeroPrototypeId = "InitialInfected";

    /// <summary>
    /// When the round will next check for round end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextRoundEndCheck;

    /// <summary>
    /// The amount of time between each check for the end of the round.
    /// </summary>
    [DataField]
    public TimeSpan EndCheckDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The time at which the initial infected will be chosen.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? StartTime;

    /// <summary>
    /// The minimum amount of time after the round starts that the initial infected will be chosen.
    /// </summary>
    [DataField]
    public TimeSpan MinStartDelay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The maximum amount of time after the round starts that the initial infected will be chosen.
    /// </summary>
    [DataField]
    public TimeSpan MaxStartDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The sound that plays when someone becomes an initial infected.
    /// todo: this should have a unique sound instead of reusing the zombie one.
    /// </summary>
    [DataField]
    public SoundSpecifier InitialInfectedSound = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");

    /// <summary>
    /// The minimum amount of time initial infected have before they start taking infection damage.
    /// </summary>
    [DataField]
    public TimeSpan MinInitialInfectedGrace = TimeSpan.FromMinutes(12.5f);

    /// <summary>
    /// The maximum amount of time initial infected have before they start taking damage.
    /// </summary>
    [DataField]
    public TimeSpan MaxInitialInfectedGrace = TimeSpan.FromMinutes(15f);

    /// <summary>
    /// How many players for each initial infected.
    /// </summary>
    [DataField]
    public int PlayersPerInfected = 10;

    /// <summary>
    /// The maximum number of initial infected.
    /// </summary>
    [DataField]
    public int MaxInitialInfected = 6;

    /// <summary>
    /// After this amount of the crew become zombies, the shuttle will be automatically called.
    /// </summary>
    [DataField]
    public float ZombieShuttleCallPercentage = 0.7f;

    [DataField]
    public EntProtoId ZombifySelfActionPrototype = "ActionTurnUndead";
}
