using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles baseline plant progression each growth tick: aging, resource consumption,
/// simple viability checks.
/// </summary>
public sealed class BasicGrowthSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BasicGrowthComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<BasicGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnCrossPollinate(Entity<BasicGrowthComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<BasicGrowthComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossFloat(ref ent.Comp.WaterConsumption, pollenData.WaterConsumption);
        _mutation.CrossFloat(ref ent.Comp.NutrientConsumption, pollenData.NutrientConsumption);
    }

    private void OnPlantGrow(Entity<BasicGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (plantUid, plantComp) = ent;
        var trayUid = GetEntity(args.Tray);

        if (!TryComp<PlantTrayComponent>(trayUid, out var trayComp))
            return;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(plantUid).Id);
        var rand = new System.Random(seed);

        // Advance plant age here.
        if (holder.SkipAging > 0)
            _plantHolder.AdjustsSkipAging(plantUid, -1);
        else if (rand.Prob(0.8f))
            _plantHolder.AdjustsAge(plantUid, 1);

        if (plantComp.WaterConsumption > 0 && trayComp.WaterLevel > 0 && rand.Prob(0.75f))
        {
            _plantTray.AdjustWater(trayUid,-MathF.Max(0f, plantComp.WaterConsumption * trayComp.TrayConsumptionMultiplier));
        }

        if (plantComp.NutrientConsumption > 0 && trayComp.NutritionLevel > 0 && rand.Prob(0.75f))
        {
            _plantTray.AdjustNutrient(trayUid,  -MathF.Max(0f, plantComp.NutrientConsumption * trayComp.TrayConsumptionMultiplier));
        }

        var healthMod = rand.Next(1, 3);
        if (holder.SkipAging < 10)
        {
            // Make sure the plant is not thirsty.
            if (trayComp.WaterLevel > 10)
            {
                _plantHolder.AdjustsHealth(plantUid, (rand.Prob(0.35f) ? 1 : 0) * healthMod);
            }
            else
            {
                _plantHarvest.AffectGrowth(plantUid, -1);
                _plantHolder.AdjustsHealth(plantUid, -healthMod);
            }

            if (trayComp.NutritionLevel > 5)
            {
                _plantHolder.AdjustsHealth(plantUid, (rand.Prob(0.35f) ? 1 : 0) * healthMod);
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
    public void AdjustWaterConsumption(Entity<BasicGrowthComponent?> ent, float amount)
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
    public void AdjustNutrientConsumption(Entity<BasicGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.NutrientConsumption = MathF.Max(0f, ent.Comp.NutrientConsumption + amount);
        DirtyField(ent, nameof(ent.Comp.NutrientConsumption));
    }
}
