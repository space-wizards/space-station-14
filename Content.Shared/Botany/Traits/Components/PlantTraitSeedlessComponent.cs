using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// A plant trait that prevents a plant from producing seeds using a seed maker.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlantTraitSeedlessComponent : PlantTraitsComponent
{
    public PlantTraitSeedlessComponent()
    {
        TraitState = "mutation-plant-seedless";
    }
}
