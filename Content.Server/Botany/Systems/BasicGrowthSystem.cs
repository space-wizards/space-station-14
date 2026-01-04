using JetBrains.Annotations;
using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles baseline plant progression each growth tick: aging, resource consumption,
/// simple viability checks, and basic swab cross-pollination behavior.
/// </summary>
public sealed class BasicGrowthSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BasicGrowthComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<BasicGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnCrossPollinate(Entity<BasicGrowthComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<BasicGrowthComponent>(args.PollenData,
                args.PollenProtoId,
                out var pollenData))
            return;

        _mutation.CrossFloat(ref ent.Comp.WaterConsumption, pollenData.WaterConsumption);
        _mutation.CrossFloat(ref ent.Comp.NutrientConsumption, pollenData.NutrientConsumption);
    }

    private void OnPlantGrow(Entity<BasicGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (plantUid, plantComp) = ent;
        var (trayUid, trayComp) = args.Tray;

        if (trayComp == null)
            return;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        // Advance plant age here.
        if (holder.SkipAging > 0)
            _plantHolder.AdjustsSkipAging(plantUid, -1);
        else if (_random.Prob(0.8f))
            _plantHolder.AdjustsAge(plantUid, 1);

        if (plantComp.WaterConsumption > 0 && trayComp.WaterLevel > 0 && _random.Prob(0.75f))
        {
            _plantTray.AdjustWater(trayUid,-MathF.Max(0f, plantComp.WaterConsumption * trayComp.TrayConsumptionMultiplier));
        }

        if (plantComp.NutrientConsumption > 0 && trayComp.NutritionLevel > 0 && _random.Prob(0.75f))
        {
            _plantTray.AdjustNutrient(trayUid,  -MathF.Max(0f, plantComp.NutrientConsumption * trayComp.TrayConsumptionMultiplier));

            _plant.UpdateSprite(plantUid);
        }

        var healthMod = _random.Next(1, 3);
        if (holder.SkipAging < 10)
        {
            // Make sure the plant is not thirsty.
            if (trayComp.WaterLevel > 10)
            {
                _plantHolder.AdjustsHealth(plantUid, Convert.ToInt32(_random.Prob(0.35f)) * healthMod);
            }
            else
            {
                _plantHarvest.AffectGrowth(plantUid, -1);
                _plantHolder.AdjustsHealth(plantUid, -healthMod);
            }

            if (trayComp.NutritionLevel > 5)
            {
                _plantHolder.AdjustsHealth(plantUid, Convert.ToInt32(_random.Prob(0.35f)) * healthMod);
            }
            else
            {
                _plantHarvest.AffectGrowth(plantUid, -1);
                _plantHolder.AdjustsHealth(plantUid, -healthMod);
            }
        }
    }
}

/// <summary>
/// Event of plant growing ticking.
/// </summary>
[ByRefEvent]
public readonly record struct OnPlantGrowEvent(Entity<PlantTrayComponent?> Tray);
