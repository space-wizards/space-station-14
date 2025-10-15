using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;

[ByRefEvent]
public readonly record struct OnPlantGrowEvent;

/// <summary>
/// Base for botany growth systems, providing shared helpers and constants used by
/// per-trait/per-environment growth handlers.
/// </summary>
public abstract class PlantGrowthSystem : EntitySystem
{
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

        if (!TryComp<PlantTraitsComponent>(uid, out var traits))
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
