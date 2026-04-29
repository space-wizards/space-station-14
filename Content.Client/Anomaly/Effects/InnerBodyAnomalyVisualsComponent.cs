using Content.Shared.DisplacementMap;

namespace Content.Client.Anomaly.Effects;

/// <summary>
/// Complementary visuals component to <see cref="InnerBodyAnomalyVisualsComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class InnerBodyAnomalyVisualsComponent : Component
{
    /// <summary>
    /// If set, applies a displacement map to the anomaly sprites.
    /// </summary>
    [DataField]
    public DisplacementData? Displacement;
}
