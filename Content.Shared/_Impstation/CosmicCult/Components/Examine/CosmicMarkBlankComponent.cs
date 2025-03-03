using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.CosmicCult.Components.Examine;

/// <summary>
/// Marker component for targets under the effect of Shunt Subjectivity or Astral Projection.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicMarkBlankComponent : Component
{
    /// <summary>
    /// The status icon corresponding to the effect.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SsdIconPrototype> StatusIcon { get; set; } = "CosmicSSDIcon";
}
