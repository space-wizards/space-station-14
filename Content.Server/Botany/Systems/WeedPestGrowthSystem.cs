using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Manages weed growth and pest damage per growth tick, and handles tray-level
/// weed spawning and kudzu transformation based on conditions.
/// </summary>
public sealed class WeedPestGrowthSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WeedPestGrowthComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<WeedPestGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantTrayComponent, OnPlantGrowEvent>(OnTrayUpdate);
    }

    private void OnCrossPollinate(Entity<WeedPestGrowthComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<WeedPestGrowthComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossFloat(ref ent.Comp.WeedTolerance, pollenData.WeedTolerance);
        _mutation.CrossFloat(ref ent.Comp.PestTolerance, pollenData.PestTolerance);
    }

    private void OnPlantGrow(Entity<WeedPestGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (plantUid, component) = ent;

        if (args.Tray.Comp == null)
            return;

        // Weed growth.
        if (_random.Prob(component.WeedGrowthChance))
            args.Tray.Comp.WeedLevel += component.WeedGrowthAmount;

        // Pest damage.
        if (_random.Prob(component.PestDamageChance))
            _plantHolder.AdjustsHealth(plantUid, -component.PestDamageAmount);
    }

    /// <summary>
    /// Handles weed growth and kudzu transformation for plant holder trays.
    /// </summary>
    private void OnTrayUpdate(Entity<PlantTrayComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        if (_plantTray.TryGetPlant(ent.AsNullable(), out var plant))
        {
            if (!TryComp(plant, out PlantTraitsComponent? traits)
                || !TryComp(plant, out WeedPestGrowthComponent? weed)
                || !TryComp(plant, out PlantHolderComponent? holder))
                return;

            // Weeds like water and nutrients! They may appear even if there's not a seed planted.
            if (component is { WaterLevel: > 10, NutritionLevel: > 5 })
            {
                float chance;
                if (traits.TurnIntoKudzu)
                    chance = 1f;
                else
                    chance = 0.01f;

                if (_random.Prob(chance))
                    component.WeedLevel += 1 + component.WeedCoefficient;
            }

            // Handle kudzu transformation.
            if (traits.TurnIntoKudzu
                && component.WeedLevel >= weed.WeedHighLevelThreshold)
            {
                Spawn(traits.KudzuPrototype, Transform(uid).Coordinates.SnapToGrid(EntityManager));
                traits.TurnIntoKudzu = false;
                holder.Health = 0;
            }
        }
        else
        {
            if (component is { WaterLevel: > 10, NutritionLevel: > 5 })
            {
                if (_random.Prob(0.05f))
                    component.WeedLevel += 1 + component.WeedCoefficient;
            }
        }
    }
}
