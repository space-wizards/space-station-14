using Content.Server.Botany.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

[ByRefEvent]
public readonly record struct OnPlantGrowEvent;

/// <summary>
/// Base system for plant growth mechanics. Provides common functionality for all plant growth systems.
/// </summary>
public abstract class PlantGrowthSystem : EntitySystem
{
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] protected readonly IGameTiming _gameTiming = default!;

    /// <summary>
    /// Multiplier for plant growth speed in hydroponics.
    /// </summary>
    public const float HydroponicsSpeedMultiplier = 1f;

    /// <summary>
    /// Multiplier for resource consumption (water, nutrients) in hydroponics.
    /// </summary>
    public const float HydroponicsConsumptionMultiplier = 2f;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Affects the growth of a plant by modifying its age or production timing.
    /// </summary>
    public void AffectGrowth(EntityUid uid, int amount, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.Seed == null)
            return;

        PlantTraitsComponent? traits = null;
        Resolve<PlantTraitsComponent>(uid, ref traits);

        if (traits == null)
            return;

        // Synchronize harvest status with HarvestComponent if present
        if (TryComp<HarvestComponent>(uid, out var harvestComp))
        {
            component.Harvest = harvestComp.ReadyForHarvest;
        }

        if (amount > 0)
        {
            if (component.Age < traits.Maturation)
                component.Age += amount;
            else if (!component.Harvest && traits.Yield <= 0f)
                component.LastProduce -= amount;
        }
        else
        {
            if (component.Age < traits.Maturation)
                component.SkipAging++;
            else if (!component.Harvest && traits.Yield <= 0f)
                component.LastProduce += amount;
        }
    }
}
