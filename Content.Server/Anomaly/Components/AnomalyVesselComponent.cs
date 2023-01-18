using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
    [ViewVariables]
    public EntityUid? Anomaly;

    /// <summary>
    /// A multiplier applied to the amount of points generated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float PointMultiplier = 1;

    /// <summary>
    /// The machine part that affects the point multiplier of the vessel
    /// </summary>
    [DataField("machinePartPointModifier", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartPointModifier = "ScanningModule";

    /// <summary>
    /// A value used to scale the point multiplier
    /// with the corresponding part rating.
    /// </summary>
    [DataField("partRatingPointModifier")]
    public float PartRatingPointModifier = 1.5f;
}
