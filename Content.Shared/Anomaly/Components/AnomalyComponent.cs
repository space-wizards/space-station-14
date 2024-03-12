using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// This is used for tracking the general behavior of anomalies.
/// This doesn't contain the specific implementations for what
/// they do, just the generic behaviors associated with them.
///
/// Anomalies and their related components were designed here: https://hackmd.io/@ss14-design/r1sQbkJOs
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedAnomalySystem))]
public sealed partial class AnomalyComponent : Component
{
    /// <summary>
    /// How likely an anomaly is to grow more dangerous. Moves both up and down.
    /// Ranges from 0 to 1.
    /// Values less than 0.5 indicate stability, whereas values greater
    /// than 0.5 indicate instability, which causes increases in severity.
    /// </summary>
    /// <remarks>
    /// Note that this doesn't refer to stability as a percentage: This is an arbitrary
    /// value that only matters in relation to the <see cref="GrowthThreshold"/> and <see cref="DecayThreshold"/>
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Stability = 0f;

    /// <summary>
    /// How severe the effects of an anomaly are. Moves only upwards.
    /// Ranges from 0 to 1.
    /// A value of 0 indicates effects of extrememly minimal severity, whereas greater
    /// values indicate effects of linearly increasing severity.
    /// </summary>
    /// <remarks>
    /// Wacky-Stability scale lives on in my heart. - emo
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Severity = 0f;

    #region Health
    /// <summary>
    /// The internal "health" of an anomaly.
    /// Ranges from 0 to 1.
    /// When the health of an anomaly reaches 0, it is destroyed without ever
    /// reaching a supercritical point.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Health = 1f;

    /// <summary>
    /// If the <see cref="Stability"/> of the anomaly exceeds this value, it
    /// becomes too unstable to support itself and starts decreasing in <see cref="Health"/>.
    /// </summary>
    [DataField("decayhreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float DecayThreshold = 0.15f;

    /// <summary>
    /// The amount of health lost when the stability is below the <see cref="DecayThreshold"/>
    /// </summary>
    [DataField("healthChangePerSecond"), ViewVariables(VVAccess.ReadWrite)]
    public float HealthChangePerSecond = -0.01f;
    #endregion

    #region Growth
    /// <summary>
    /// If the <see cref="Stability"/> of the anomaly exceeds this value, it
    /// becomes unstable and starts increasing in <see cref="Severity"/>.
    /// </summary>
    [DataField("growthThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float GrowthThreshold = 0.5f;

    /// <summary>
    /// A coefficient used for calculating the increase in severity when above the GrowthThreshold
    /// </summary>
    [DataField("severityGrowthCoefficient"), ViewVariables(VVAccess.ReadWrite)]
    public float SeverityGrowthCoefficient = 0.07f;
    #endregion

    #region Pulse
    /// <summary>
    /// The time at which the next artifact pulse will occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPulseTime = TimeSpan.Zero;

    /// <summary>
    /// The minimum interval between pulses.
    /// </summary>
    [DataField]
    public TimeSpan MinPulseLength = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The maximum interval between pulses.
    /// </summary>
    [DataField]
    public TimeSpan MaxPulseLength = TimeSpan.FromMinutes(2);

    /// <summary>
    /// A percentage by which the length of a pulse might vary.
    /// </summary>
    [DataField]
    public float PulseVariation = 0.1f;

    /// <summary>
    /// The range that an anomaly's stability can vary each pulse. Scales with severity.
    /// </summary>
    /// <remarks>
    /// This is more likely to trend upwards than donwards, because that's funny
    /// </remarks>
    [DataField]
    public Vector2 PulseStabilityVariation = new(-0.1f, 0.15f);

    /// <summary>
    /// The sound played when an anomaly pulses
    /// </summary>
    [DataField]
    public SoundSpecifier? PulseSound = new SoundCollectionSpecifier("RadiationPulse");

    /// <summary>
    /// The sound plays when an anomaly goes supercritical
    /// </summary>
    [DataField]
    public SoundSpecifier? SupercriticalSound = new SoundCollectionSpecifier("Explosion");
    #endregion

    /// <summary>
    /// The range of initial values for stability
    /// </summary>
    /// <remarks>
    /// +/- 0.2 from perfect stability (0.5)
    /// </remarks>
    [DataField]
    public (float, float) InitialStabilityRange = (0.4f, 0.6f);

    /// <summary>
    /// The range of initial values for severity
    /// </summary>
    /// <remarks>
    /// Between 0 and 0.5, which should be all mild effects
    /// </remarks>
    [DataField]
    public (float, float) InitialSeverityRange = (0.1f, 0.5f);

    /// <summary>
    /// The particle type that increases the severity of the anomaly.
    /// </summary>
    [DataField]
    public AnomalousParticleType SeverityParticleType;

    /// <summary>
    /// The particle type that destabilizes the anomaly.
    /// </summary>
    [DataField]
    public AnomalousParticleType DestabilizingParticleType;

    /// <summary>
    /// The particle type that weakens the anomalys health.
    /// </summary>
    [DataField]
    public AnomalousParticleType WeakeningParticleType;

    #region Points and Vessels
    /// <summary>
    /// The vessel that the anomaly is connceted to. Stored so that multiple
    /// vessels cannot connect to the same anomaly.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ConnectedVessel;

    /// <summary>
    /// The minimum amount of research points generated per second
    /// </summary>
    [DataField("minPointsPerSecond")]
    public int MinPointsPerSecond = 10;

    /// <summary>
    /// The maximum amount of research points generated per second
    /// This doesn't include the point bonus for being unstable.
    /// </summary>
    [DataField("maxPointsPerSecond")]
    public int MaxPointsPerSecond = 80;

    /// <summary>
    /// The multiplier applied to the point value for the
    /// anomaly being above the <see cref="GrowthThreshold"/>
    /// </summary>
    [DataField("growingPointMultiplier")]
    public float GrowingPointMultiplier = 1.5f;
    #endregion

    /// <summary>
    /// The amount of damage dealt when either a player touches the anomaly
    /// directly or by hitting the anomaly.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier AnomalyContactDamage = default!;

    /// <summary>
    /// The sound effect played when a player
    /// burns themselves on an anomaly via contact.
    /// </summary>
    [DataField]
    public SoundSpecifier AnomalyContactDamageSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    /// <summary>
    /// A prototype entity that appears when an anomaly supercrit collapse.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? CorePrototype;

    /// <summary>
    /// A prototype entity that appears when an anomaly decays.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? CoreInertPrototype;

    #region Floating Animation
    /// <summary>
    /// How long it takes to go from the bottom of the animation to the top.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("animationTime")]
    public float AnimationTime = 2f;

    /// <summary>
    /// How far it goes in any direction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset")]
    public Vector2 FloatingOffset = new(0, 0.15f);

    public readonly string AnimationKey = "anomalyfloat";
    #endregion
}

/// <summary>
/// Event raised at regular intervals on an anomaly to do whatever its effect is.
/// </summary>
/// <param name="Anomaly">The anomaly pulsing</param>
/// <param name="Stability"></param>
/// <param name="Severity"></param>
[ByRefEvent]
public readonly record struct AnomalyPulseEvent(EntityUid Anomaly, float Stability, float Severity);

/// <summary>
/// Event raised on an anomaly when it reaches a supercritical point.
/// </summary>
[ByRefEvent]
public readonly record struct AnomalySupercriticalEvent(EntityUid Anomaly);

/// <summary>
/// Event broadcast after an anomaly goes supercritical
/// </summary>
/// <param name="Anomaly">The anomaly being shut down.</param>
/// <param name="Supercritical">Whether or not the anomaly shut down passively or via a supercritical event.</param>
[ByRefEvent]
public readonly record struct AnomalyShutdownEvent(EntityUid Anomaly, bool Supercritical);

/// <summary>
/// Event broadcast when an anomaly's severity is changed.
/// </summary>
/// <param name="Anomaly">The anomaly being changed</param>
[ByRefEvent]
public readonly record struct AnomalySeverityChangedEvent(EntityUid Anomaly, float Stability, float Severity);

/// <summary>
/// Event broadcast when an anomaly's stability is changed.
/// </summary>
[ByRefEvent]
public readonly record struct AnomalyStabilityChangedEvent(EntityUid Anomaly, float Stability, float Severity);

/// <summary>
/// Event broadcast when an anomaly's health is changed.
/// </summary>
/// <param name="Anomaly">The anomaly being changed</param>
[ByRefEvent]
public readonly record struct AnomalyHealthChangedEvent(EntityUid Anomaly, float Health);
