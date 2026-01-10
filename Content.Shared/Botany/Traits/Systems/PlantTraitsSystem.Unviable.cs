using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitUnviableComponent"/>
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
