using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Server.Popups;
using Robust.Shared.Localization;

namespace Content.Server.Botany.Systems;

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

    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    public override void OnPlantGrow(Entity<PlantTraitsComponent> ent, ref OnPlantGrowEvent args)
    {
        _plantHarvest.AffectGrowth(ent.Owner, -1);
        _plantHolder.AdjustsHealth(ent.Owner, -UnviableDamage);
    }

    public override IEnumerable<string> GetPlantStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-unviable");
    }
}
