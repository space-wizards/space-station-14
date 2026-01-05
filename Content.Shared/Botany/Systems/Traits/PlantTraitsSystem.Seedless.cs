namespace Content.Shared.Botany.Systems;

/// <summary>
/// The produce of the plant become seedless, which makes it impossible to extract seeds from them.
/// </summary>
[DataDefinition]
public sealed partial class TraitSeedless : PlantTrait
{
    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-seedless");
    }
}
