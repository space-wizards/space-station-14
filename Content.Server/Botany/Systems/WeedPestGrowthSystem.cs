using Content.Server.Botany.Components;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Manages weed growth and pest damage per growth tick, and handles tray-level
/// weed spawning and kudzu transformation based on conditions.
/// </summary>
public sealed class WeedPestGrowthSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WeedPestGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantHolderComponent, OnPlantGrowEvent>(OnTrayUpdate);
    }

    private void OnPlantGrow(Entity<WeedPestGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        if (!TryComp(uid, out PlantHolderComponent? holder))
            return;

        // Weed growth logic.
        if (_random.Prob(component.WeedGrowthChance))
        {
            holder.WeedLevel += component.WeedGrowthAmount;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        // Pest damage logic.
        if (_random.Prob(component.PestDamageChance))
            holder.Health -= component.PestDamageAmount;
    }

    /// <summary>
    /// Handles weed growth and kudzu transformation for plant holder trays.
    /// </summary>
    private void OnTrayUpdate(Entity<PlantHolderComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        if (!TryComp(uid, out PlantTraitsComponent? traits)
            || !TryComp(uid, out WeedPestGrowthComponent? weed))
            return;

        // Weeds like water and nutrients! They may appear even if there's not a seed planted.
        if (component is { WaterLevel: > 10, NutritionLevel: > 5 })
        {
            float chance;
            if (component.Seed == null)
                chance = 0.05f;
            else if (traits.TurnIntoKudzu)
                chance = 1f;
            else
                chance = 0.01f;

            if (_random.Prob(chance))
                component.WeedLevel += 1 + component.WeedCoefficient * BasicGrowthSystem.HydroponicsSpeedMultiplier;

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        // Handle kudzu transformation.
        if (component is { Seed: not null, Dead: false }
            && traits.TurnIntoKudzu
            && component.WeedLevel >= weed.WeedHighLevelThreshold)
        {
            Spawn(traits.KudzuPrototype, Transform(uid).Coordinates.SnapToGrid(EntityManager));
            traits.TurnIntoKudzu = false;
            component.Health = 0;
        }
    }
}
