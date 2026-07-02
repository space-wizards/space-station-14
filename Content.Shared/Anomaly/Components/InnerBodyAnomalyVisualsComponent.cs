using Content.Shared.DisplacementMap;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// Complementary visuals component to <see cref="InnerBodyAnomalyComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InnerBodyAnomalyVisualsComponent : Component
{
    /// <summary>
    /// If set, applies a displacement map to the anomaly sprites.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DisplacementDataPrototype>? Displacement;
}
