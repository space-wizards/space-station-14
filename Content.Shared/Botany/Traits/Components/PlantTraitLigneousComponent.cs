using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// A plant trait that causes a plant to become ligneous, preventing it from being harvested without special tools.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlantTraitLigneousComponent : PlantTraitsComponent
{
    /// <summary>
    /// Tool quality that required if plant should be harvested with specified tool.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype>? HarvestToolQuality = "Sawing";

    [DataField]
    public override LocId TraitState { get; set; } = "mutation-plant-ligneous";
}
