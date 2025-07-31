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

        if (holder.Harvest && _random.Prob(component.HarvestChance))
        {
            // Auto-harvest the plant
            holder.Harvest = false;
            holder.LastProduce = holder.Age;

            // Spawn the harvested items
            if (holder.Seed.ProductPrototypes.Count > 0)
            {
                var product = _random.Pick(holder.Seed.ProductPrototypes);
                var entity = Spawn(product, Transform(uid).Coordinates);
            }
        }
    }
}
