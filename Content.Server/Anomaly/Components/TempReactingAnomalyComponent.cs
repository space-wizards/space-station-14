using Robust.Shared.Utility;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// Causes the anomaly to react to different temperatures.
/// </summary>
/// <remarks>
/// This could later be extended to react only to specific gasses.
/// </remarks>
[RegisterComponent]
public sealed class TempReactingAnomalyComponent : Component
{
    /// <summary>
    /// A list of reactions that happen at certain temperatures.
    /// </summary>
    [DataField("reactions")]
    public List<TempReaction> Reactions = new();
}

/// <summary>
/// Modifies stability, severity, or health of an anomaly when temperature is within the specified range.
/// </summary>
[DataDefinition]
public sealed class TempReaction
{
    /// <summary>
    /// The lower end of the temperature range for the reaction to occur.
    /// </summary>
    [DataField("minTempKelvin")]
    public float MinTempKelvin = 0;

    /// <summary>
    /// The upper end of the temperature range for the reaction to occur.
    /// </summary>
    [DataField("maxTempKelvin")]
    public float MaxTempKelvin = float.MaxValue;

    /// <summary>
    /// The amount of stability added per second when temp is in range.
    /// </summary>
    [DataField("stabilityPerPulse")]
    public float StabilityPerPulse = 0;

    /// <summary>
    /// The amount of stability added per second when temp is in range.
    /// </summary>
    [DataField("severityPerPulse")]
    public float SeverityPerPulse = 0;

    /// <summary>
    /// The amount of stability added per second when temp is in range.
    /// </summary>
    [DataField("healthPerPulse")]
    public float HealthPerPulse = 0;

    public bool TempInRange(float temp)
    {
        DebugTools.Assert(MinTempKelvin <= MaxTempKelvin);

        return temp >= MinTempKelvin && temp < MaxTempKelvin;
    }
}
