using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitUnviableComponent"/>
public sealed partial class PlantTraitUnviableSystem : EntitySystem
{
    [Dependency] private PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantTraitUnviableComponent> ent, ref OnPlantGrowEvent args)
    {
        _plantHarvest.AffectGrowth(ent.Owner, -1);
        _plantHolder.AdjustsHealth(ent.Owner, -ent.Comp.UnviableDamage);
    }
}
