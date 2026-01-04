using Content.Server.Botany.Components;
using Content.Server.Popups;

namespace Content.Server.Botany.Systems;

/// <summary>
/// The produce of the plant become seedless, which makes it impossible to extract seeds from them.
/// </summary>
[DataDefinition]
public sealed partial class TraitSeedless : PlantTrait
{
    public override IEnumerable<string> GetPlantStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-seedless");
    }
}
