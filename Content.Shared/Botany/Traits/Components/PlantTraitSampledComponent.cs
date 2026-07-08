using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// A plant trait that indicates the plant has already been sampled, preventing it from being sampled again.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlantTraitSampledComponent : PlantTraitsComponent
{
    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-sampled");
    }
}
