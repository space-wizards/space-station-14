using Content.Shared.Atmos;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent]
public sealed class PyroclasticAnomalyComponent : Component
{
    /// <summary>
    /// The MAXIMUM amount of heat released per second.
    /// This is scaled linearly with the Severity of the anomaly.
    /// </summary>
    /// <remarks>
    /// I have no clue if this is balanced.
    /// </remarks>
    [DataField("heatPerSecond")]
    public float HeatPerSecond = 25;

    /// <summary>
    /// The maximum distance from which you can be ignited by the anomaly.
    /// </summary>
    [DataField("maximumIgnitionRadius")]
    public float MaximumIgnitionRadius = 8f;

    /// <summary>
    /// The minimum amount of severity required
    /// before the anomaly becomes a hotspot.
    /// </summary>
    [DataField("anomalyHotspotThreshold")]
    public float AnomalyHotspotThreshold = 0.6f;

    /// <summary>
    /// The temperature of the hotspot where the anomaly is
    /// </summary>
    [DataField("hotspotExposeTemperature")]
    public float HotspotExposeTemperature = 1000;

    /// <summary>
    /// The volume of the hotspot where the anomaly is.
    /// </summary>
    [DataField("hotspotExposeVolume")]
    public float HotspotExposeVolume = 50;

    /// <summary>
    /// Gas released when the anomaly goes supercritical.
    /// </summary>
    [DataField("supercriticalGas")]
    public Gas SupercriticalGas = Gas.Plasma;

    /// <summary>
    /// The amount of gas released when the anomaly goes supercritical
    /// </summary>
    [DataField("supercriticalMoleAmount")]
    public float SupercriticalMoleAmount = 75f;
}
