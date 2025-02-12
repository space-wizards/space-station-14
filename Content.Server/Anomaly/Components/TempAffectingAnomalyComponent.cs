namespace Content.Server.Anomaly.Components;

/// <summary>
/// This component is used for handling anomalies that affect the temperature
/// </summary>
[RegisterComponent]
public sealed partial class TempAffectingAnomalyComponent : Component
{

    /// <summary>
    /// The the amount the tempurature should be modified by (negative for decreasing temp)
    /// </summary>
    [DataField]
    public float TempChangePerSecond = 0;

    /// <summary>
    /// The minimum amount of severity required
    /// before the anomaly becomes a hotspot.
    /// </summary>
    [DataField]
    public float AnomalyHotSpotThreshold = 0.6f;

    /// <summary>
    /// The temperature of the hotspot where the anomaly is
    /// </summary>
    [DataField]
    public float HotspotExposeTemperature = 0;

    /// <summary>
    /// The volume of the hotspot where the anomaly is.
    /// </summary>
    [DataField]
    public float HotspotExposeVolume = 50;
}
