using Content.Shared.Slippery;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// A plant trait that makes harvested produce slippery.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlantTraitSlipperyComponent : PlantTraitsComponent
{
    /// <inheritdoc/>
    public override LocId? TraitState { get; set; } = "mutation-plant-slippery";

    /// <inheritdoc/>
    public override LocId? TraitName { get; set; } = "mutation-plant-noun-slippery";
}
