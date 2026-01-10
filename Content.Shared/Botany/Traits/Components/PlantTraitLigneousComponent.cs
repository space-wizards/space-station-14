using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// A plant trait that causes a plant to become ligneous, preventing it from being harvested without special tools.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlantTraitLigneousComponent : PlantTraitsComponent
{
    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-ligneous");
    }
}
