using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PlantTraitLigneousComponent : PlantTraitsComponent
{
    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-ligneous");
    }
}
