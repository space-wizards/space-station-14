using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Marker component for targets under the effect of Shunt Subjectivity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicShuntedOriginComponent : Component
{
    /// <summary>
    /// The status icon corresponding to the effect.
    /// </summary>
    [DataField]
    public ProtoId<SsdIconPrototype> StatusIcon = "CosmicSSDIcon";
}
