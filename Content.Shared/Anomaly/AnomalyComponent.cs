namespace Content.Shared.Anomaly;

/// <summary>
/// This is used for tracking the general behavior of anomalies.
/// This doesn't contain the specific implementations for what
/// they do, just the generic behaviors associated with them.
/// </summary>
[RegisterComponent]
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
    /// value that only matters in relation to the <see cref="GrowthThreshold"/> and <see cref="DeathThreshold"/>
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
    [DataField("deathThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float DeathThreshold = 0.15f;

    /// <summary>
    /// The amount of health lost when the stability is below the <see cref="DeathThreshold"/>
    /// </summary>
    [DataField("healthChangePerSecond"), ViewVariables(VVAccess.ReadWrite)]
    public float HealthChangePerSecond = -0.05f;
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

    /// <summary>
    /// Whether or not the anomaly has gone supercritical
    /// </summary>
    [ViewVariables]
    public bool Supercritical = false;

    [ViewVariables]
    public TimeSpan NextSecondUpdate = TimeSpan.Zero;
    #endregion

    #region Pulse
    /// <summary>
    /// The time at which the next artifact pulse will occur.
    /// </summary>
    [ViewVariables]
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
    #endregion

    #region Starting Values
    [DataField("initialStabilityRange")]
    public (float, float) InitialStabilityRange = (0.2f, 0.5f);

    [DataField("initialSeverityRange")]
    public (float, float) InitialSeverityRange = (0.0f, 0.35f);
    #endregion
}


/// <summary>
/// Event raised at regular intervals on an anomaly to do whatever its effect is.
/// </summary>
/// <param name="Stabiltiy"></param>
/// <param name="Severity"></param>
[ByRefEvent]
public readonly record struct AnomalyPulseEvent(float Stabiltiy, float Severity)
{
    public readonly float Stabiltiy = Stabiltiy;
    public readonly float Severity = Severity;
}

/// <summary>
/// Event raised on an anomaly when it reaches a supercritical point.
/// </summary>
[ByRefEvent]
public readonly record struct AnomalySupercriticalEvent;

/// <summary>
/// Event broadcast after an anomaly goes supercritical
/// </summary>
/// <param name="Anomaly"></param>
[ByRefEvent]
public readonly record struct AnomalyShutdownEvent(EntityUid Anomaly, bool Supercritical)
{
    /// <summary>
    /// The anomaly being shut down.
    /// </summary>
    public readonly EntityUid Anomaly = Anomaly;

    /// <summary>
    /// Whether or not the anomaly shut down passively
    /// or via a supercritical event.
    /// </summary>
    /// <returns></returns>
    public readonly bool Supercritical = Supercritical;
}
