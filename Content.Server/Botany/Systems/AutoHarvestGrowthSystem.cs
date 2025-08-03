using Content.Server.Botany.Components;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

public sealed class AutoHarvestGrowthSystem : PlantGrowthSystem
{
    [Dependency] private readonly HarvestSystem _harvestSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoHarvestGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(EntityUid uid, AutoHarvestGrowthComponent component, OnPlantGrowEvent args)
    {
        PlantHolderComponent? holder = null;
        Resolve<PlantHolderComponent>(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead)
            return;

        // Check if ready for harvest using HarvestComponent
        if (TryComp<HarvestComponent>(uid, out var harvestComp) && harvestComp.ReadyForHarvest && _random.Prob(component.HarvestChance))
        {
            _harvestSystem.DoHarvest(uid, uid, harvestComp, holder);
        }
    }
}
