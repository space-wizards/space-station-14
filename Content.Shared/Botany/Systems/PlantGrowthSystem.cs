using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles baseline plant progression each growth tick: aging, resource consumption,
/// simple viability checks.
/// </summary>
public sealed partial class PlantGrowthSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;
    [Dependency] private PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;

    [SubscribeLocalEvent]
    private void OnCrossPollinate(Entity<PlantGrowthComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantGrowthComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossFloat(ent, ref ent.Comp.WaterConsumption, pollenData.WaterConsumption);
        _mutation.CrossFloat(ent, ref ent.Comp.NutrientConsumption, pollenData.NutrientConsumption);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantGrowthComponent> ent, ref PlantGrowEvent args)
    {
        var (plantUid, plantComp) = ent;
        var trayUid = GetEntity(args.Tray);

        if (!TryComp<PlantTrayComponent>(trayUid, out var trayComp))
            return;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(plantUid));

        // TODO: There are too many magic numbers that don't really make sense to add to the component. Balance needs to be reworked
        // Advance plant age here.
        if (holder.SkipAging > 0)
            _plantHolder.AdjustsSkipAging(plantUid, -1);
        else if (random.Prob(0.8f))
            _plantHolder.AdjustsAge(plantUid, 1);

        if (plantComp.WaterConsumption > 0 && trayComp.WaterLevel > 0 && random.Prob(0.75f))
        {
            _plantTray.AdjustWater(trayUid,-MathF.Max(0f, plantComp.WaterConsumption * trayComp.TrayConsumptionMultiplier));
        }

        if (plantComp.NutrientConsumption > 0 && trayComp.NutritionLevel > 0 && random.Prob(0.75f))
        {
            _plantTray.AdjustNutrient(trayUid,  -MathF.Max(0f, plantComp.NutrientConsumption * trayComp.TrayConsumptionMultiplier));
        }

        var healthMod = random.Next(1, 3);
        if (holder.SkipAging < 10)
        {
            // Make sure the plant is not thirsty.
            if (trayComp.WaterLevel > 10)
            {
                _plantHolder.AdjustsHealth(plantUid, (random.Prob(0.35f) ? 1 : 0) * healthMod);
            }
            else
            {
                _plantHarvest.AffectGrowth(plantUid, -1);
                _plantHolder.AdjustsHealth(plantUid, -healthMod);
            }

            if (trayComp.NutritionLevel > 5)
            {
                _plantHolder.AdjustsHealth(plantUid, (random.Prob(0.35f) ? 1 : 0) * healthMod);
            }
            else
            {
                _plantHarvest.AffectGrowth(plantUid, -1);
                _plantHolder.AdjustsHealth(plantUid, -healthMod);
            }
        }
    }

    /// <summary>
    /// Adjusts the water consumption of a plant.
    /// </summary>
    [PublicAPI]
    public void AdjustWaterConsumption(Entity<PlantGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.WaterConsumption = MathF.Max(0f, ent.Comp.WaterConsumption + amount);
        DirtyField(ent, nameof(ent.Comp.WaterConsumption));
    }

    /// <summary>
    /// Adjusts the nutrient consumption of a plant.
    /// </summary>
    [PublicAPI]
    public void AdjustNutrientConsumption(Entity<PlantGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.NutrientConsumption = MathF.Max(0f, ent.Comp.NutrientConsumption + amount);
        DirtyField(ent, nameof(ent.Comp.NutrientConsumption));
    }
}
