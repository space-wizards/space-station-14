using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// This is used for tracking the general behavior of anomalies.
/// This doesn't contain the specific implementations for what
/// they do, just the generic behaviors associated with them.
///
/// Anomalies and their related components were designed here: https://hackmd.io/@ss14-design/r1sQbkJOs
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class AnomalyComponent : Component
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
    [ViewVariables(VVAccess.ReadWrite)]
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
    [ViewVariables(VVAccess.ReadWrite)]
    public float Severity = 0f;

    #region Health
    /// <summary>
    /// The internal "health" of an anomaly.
    /// Ranges from 0 to 1.
    /// When the health of an anomaly reaches 0, it is destroyed without ever
    /// reaching a supercritical point.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
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
    [DataField("nextPulseTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPulseTime = TimeSpan.MaxValue;

    /// <summary>
    /// The minimum interval between pulses.
    /// </summary>
    [DataField("minPulseLength")]
    public TimeSpan MinPulseLength = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The maximum interval between pulses.
    /// </summary>
    [DataField("maxPulseLength")]
    public TimeSpan MaxPulseLength = TimeSpan.FromMinutes(2);

    /// <summary>
    /// A percentage by which the length of a pulse might vary.
    /// </summary>
    [DataField("pulseVariation")]
    public float PulseVariation = .1f;

    /// <summary>
    /// The largest value by which the anomaly will vary in stability for each pulse.
    /// In simple terms, every pulse, stability changes from a range of -this_value to this_value
    /// </summary>
    [DataField("pulseStabilityVariation")]
    public float PulseStabilityVariation = 0.05f;

    /// <summary>
    /// The sound played when an anomaly pulses
    /// </summary>
    [DataField("pulseSound")]
    public SoundSpecifier? PulseSound = new SoundCollectionSpecifier("RadiationPulse");

    /// <summary>
    /// The sound plays when an anomaly goes supercritical
    /// </summary>
    [DataField("supercriticalSound")]
    public SoundSpecifier? SupercriticalSound = new SoundCollectionSpecifier("explosion");
    #endregion

    /// <summary>
    /// The range of initial values for stability
    /// </summary>
    /// <remarks>
    /// +/- 0.2 from perfect stability (0.5)
    /// </remarks>
    [DataField("initialStabilityRange")]
    public (float, float) InitialStabilityRange = (0.4f, 0.6f);

    /// <summary>
    /// The range of initial values for severity
    /// </summary>
    /// <remarks>
    /// Between 0 and 0.5, which should be all mild effects
    /// </remarks>
    [DataField("initialSeverityRange")]
    public (float, float) InitialSeverityRange = (0.1f, 0.5f);

    /// <summary>
    /// The particle type that increases the severity of the anomaly.
    /// </summary>
    [DataField("severityParticleType")]
    public AnomalousParticleType SeverityParticleType;

    /// <summary>
    /// The amount that the <see cref="Severity"/> increases by when hit
    /// of an anomalous particle of <seealso cref="SeverityParticleType"/>.
    /// </summary>
    [DataField("severityPerSeverityHit")]
    public float SeverityPerSeverityHit = 0.025f;

    /// <summary>
    /// The particle type that destabilizes the anomaly.
    /// </summary>
    [DataField("destabilizingParticleType")]
    public AnomalousParticleType DestabilizingParticleType;

    /// <summary>
    /// The amount that the <see cref="Stability"/> increases by when hit
    /// of an anomalous particle of <seealso cref="DestabilizingParticleType"/>.
    /// </summary>
    [DataField("stabilityPerDestabilizingHit")]
    public float StabilityPerDestabilizingHit = 0.04f;

    /// <summary>
    /// The particle type that weakens the anomalys health.
    /// </summary>
    [DataField("weakeningParticleType")]
    public AnomalousParticleType WeakeningParticleType;

    /// <summary>
    /// The amount that the <see cref="Stability"/> increases by when hit
    /// of an anomalous particle of <seealso cref="DestabilizingParticleType"/>.
    /// </summary>
    [DataField("healthPerWeakeningeHit")]
    public float HealthPerWeakeningeHit = -0.05f;

    /// <summary>
    /// The amount that the <see cref="Stability"/> increases by when hit
    /// of an anomalous particle of <seealso cref="DestabilizingParticleType"/>.
    /// </summary>
    [DataField("stabilityPerWeakeningeHit")]
    public float StabilityPerWeakeningeHit = -0.1f;

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
    public int MinPointsPerSecond;

    /// <summary>
    /// The maximum amount of research points generated per second
    /// This doesn't include the point bonus for being unstable.
    /// </summary>
    [DataField("maxPointsPerSecond")]
    public int MaxPointsPerSecond = 100;

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
    [DataField("anomalyContactDamage", required: true)]
    public DamageSpecifier AnomalyContactDamage = default!;

    /// <summary>
    /// The sound effect played when a player
    /// burns themselves on an anomaly via contact.
    /// </summary>
    [DataField("anomalyContactDamageSound")]
    public SoundSpecifier AnomalyContactDamageSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    #region Floating Animation
    /// <summary>
    /// How long it takes to go from the bottom of the animation to the top.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("animationTime")]
    public readonly float AnimationTime = 2f;

    /// <summary>
    /// How far it goes in any direction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset")]
    public readonly Vector2 FloatingOffset = (0, 0.15f);

    public readonly string AnimationKey = "anomalyfloat";
    #endregion
}

[Serializable, NetSerializable]
public sealed class AnomalyComponentState : ComponentState
{
    public float Severity;
    public float Stability;
    public float Health;
    public TimeSpan NextPulseTime;

    public AnomalyComponentState(float severity, float stability, float health, TimeSpan nextPulseTime)
    {
        Severity = severity;
        Stability = stability;
        Health = health;
        NextPulseTime = nextPulseTime;
    }
}

/// <summary>
/// Event raised at regular intervals on an anomaly to do whatever its effect is.
/// </summary>
/// <param name="Stability"></param>
/// <param name="Severity"></param>
[ByRefEvent]
public readonly record struct AnomalyPulseEvent(float Stability, float Severity);

/// <summary>
/// Event raised on an anomaly when it reaches a supercritical point.
/// </summary>
[ByRefEvent]
public readonly record struct AnomalySupercriticalEvent;

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
public readonly record struct AnomalySeverityChangedEvent(EntityUid Anomaly, float Severity);

/// <summary>
/// Event broadcast when an anomaly's stability is changed.
/// </summary>
[ByRefEvent]
public readonly record struct AnomalyStabilityChangedEvent(EntityUid Anomaly, float Stability);

/// <summary>
/// Event broadcast when an anomaly's health is changed.
/// </summary>
/// <param name="Anomaly">The anomaly being changed</param>
[ByRefEvent]
public readonly record struct AnomalyHealthChangedEvent(EntityUid Anomaly, float Health);
