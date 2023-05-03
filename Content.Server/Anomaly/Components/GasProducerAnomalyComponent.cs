using Content.Shared.Atmos;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This component is used for handling gas producing anomalies
/// </summary>
[RegisterComponent]
public sealed class GasProducerAnomalyComponent : Component
{
    /// <summary>
    /// Should this gas be released when an anomaly reaches max severity?
    /// </summary>
    [DataField("releaseOnMaxSeverity")]
    public bool ReleaseOnMaxSeverity = false;

    /// <summary>
    /// Should this gas be released over time?
    /// </summary>
    [DataField("releasePassively")]
    public bool ReleasePassively = false; // In case there are any future anomalies that release gas passively

    /// <summary>
    /// The gas to release
    /// </summary>
    [DataField("releasedGas", required: true)]
    public Gas ReleasedGas = Gas.WaterVapor; // There is no entry for none, and Gas cannot be null

    /// <summary>
    /// The amount of gas released when the anomaly reaches max severity
    /// </summary>
    [DataField("criticalMoleAmount")]
    public float SuperCriticalMoleAmount = 150f;

    /// <summary>
    /// The amount of gas released passively
    /// </summary>
    [DataField("passiveMoleAmount")]
    public float PassiveMoleAmount = 1f;
}
