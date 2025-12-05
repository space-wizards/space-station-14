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
    public void AffectGrowth(Entity<PlantHolderComponent> ent, int amount)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        if (component.Seed == null)
            return;

        PlantHarvestComponent? harvest = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref harvest, ref traits))
            return;

        if (amount > 0)
        {
            if (component.Age < traits.Maturation)
                component.Age += amount;
            else if (!harvest.ReadyForHarvest && traits.Yield <= 0f)
                harvest.LastHarvest -= amount;
        }
        else
        {
            if (component.Age < traits.Maturation)
                component.SkipAging++;
            else if (!harvest.ReadyForHarvest && traits.Yield <= 0f)
                harvest.LastHarvest += amount;
        }
    }
}
