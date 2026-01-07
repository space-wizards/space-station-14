using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;

namespace Content.Shared.Botany.Traits.Systems;

/// <summary>
/// The plant stops growing and dies quickly.
/// Adds a bit of challenge to keep mutated plants alive via Unviable's frequency.
/// </summary>
public sealed partial class PlantTraitUnviableSystem : EntitySystem
{
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantTraitUnviableComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<PlantTraitUnviableComponent> ent, ref OnPlantGrowEvent args)
    {
        _plantHarvest.AffectGrowth(ent.Owner, -1);
        _plantHolder.AdjustsHealth(ent.Owner, -ent.Comp.UnviableDamage);
    }
}
