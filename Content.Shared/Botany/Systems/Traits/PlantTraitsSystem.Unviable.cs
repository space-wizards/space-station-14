using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// The plant stops growing and dies quickly.
/// Adds a bit of challenge to keep mutated plants alive via Unviable's frequency.
/// </summary>
[DataDefinition]
public sealed partial class TraitUnviable : PlantTrait
{
    /// <summary>
    /// Amount of damage dealt to the plant per growth tick with unviable.
    /// </summary>
    [DataField]
    public float UnviableDamage = 6f;

    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void OnPlantGrow(Entity<PlantTraitsComponent> ent, ref OnPlantGrowEvent args)
    {
        _plantHarvest.AffectGrowth(ent.Owner, -1);
        _plantHolder.AdjustsHealth(ent.Owner, -UnviableDamage);
    }

    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-unviable");
    }
}
