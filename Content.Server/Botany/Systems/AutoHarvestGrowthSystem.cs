using Content.Server.Botany.Components;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

public sealed class AutoHarvestGrowthSystem : PlantGrowthSystem
{
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
            // Auto-harvest the plant
            harvestComp.ReadyForHarvest = false;
            harvestComp.LastHarvestTime = holder.Age;

            // Spawn the harvested items
            if (holder.Seed.ProductPrototypes.Count > 0)
            {
                var product = _random.Pick(holder.Seed.ProductPrototypes);
                var entity = Spawn(product, Transform(uid).Coordinates);
            }
        }
    }
}
