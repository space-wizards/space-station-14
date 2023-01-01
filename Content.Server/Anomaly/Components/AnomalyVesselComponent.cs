namespace Content.Server.Anomaly.Components;

/// <summary>
/// Anomaly Vessels can have an anomaly "stored" in them
/// by interacting on them with an anomaly scanner. Then,
/// they generate points for the selected server based on
/// the anomaly's stability and severity.
/// </summary>
[RegisterComponent]
public sealed class AnomalyVesselComponent : Component
{
    /// <summary>
    /// The anomaly that the vessel is storing.
    /// Can be null.
    /// </summary>
    public EntityUid? Anomaly;

    /// <summary>
    /// A multiplier applied to the amount of points generated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float PointMultiplier = 1;

    //TODO: machine upgrades (just do a multiplier)
}
