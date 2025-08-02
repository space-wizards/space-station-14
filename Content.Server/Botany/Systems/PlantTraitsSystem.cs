using Content.Server.Botany.Components;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// System that handles plant traits like lifespan, maturation, production, yield, potency, and growth stages.
/// </summary>
public sealed class PlantTraitsSystem : PlantGrowthSystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantTraitsComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(EntityUid uid, PlantTraitsComponent component, OnPlantGrowEvent args)
    {
        PlantHolderComponent? holder = null;
        Resolve<PlantHolderComponent>(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead)
            return;

        // Check if plant is too old
        if (holder.Age > component.Lifespan)
        {
            holder.Health -= _random.Next(3, 5) * HydroponicsSpeedMultiplier;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        // Check if plant is ready for harvest
        if (holder.Seed.ProductPrototypes.Count > 0 && TryComp<HarvestComponent>(uid, out var harvestComp))
        {
            if (holder.Age > component.Production)
            {
                if (holder.Age - harvestComp.LastHarvestTime > component.Production && !harvestComp.ReadyForHarvest)
                {
                    harvestComp.ReadyForHarvest = true;
                    harvestComp.LastHarvestTime = holder.Age;
                }
            }
            else
            {
                if (harvestComp.ReadyForHarvest)
                {
                    harvestComp.ReadyForHarvest = false;
                    harvestComp.LastHarvestTime = holder.Age;
                }
            }
        }
    }
}
