using Content.Server.Botany.Components;
using Content.Shared.Swab;
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
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    // TODO: Multipliers should be taken from the hydroponics component.
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
        SubscribeLocalEvent<BasicGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<BasicGrowthComponent, BotanySwabDoAfterEvent>(OnSwab);
    }

    private void OnSwab(Entity<BasicGrowthComponent> ent, ref BotanySwabDoAfterEvent args)
    {
        var component = ent.Comp;

        if (args.Cancelled || args.Handled || args.Used == null)
            return;

        if (!TryComp<BotanySwabComponent>(args.Used.Value, out var swab) || swab.SeedData == null)
            return;

        var swabComp = swab.SeedData.GrowthComponents.BasicGrowth;
        if (swabComp == null)
        {
            swab.SeedData.GrowthComponents.BasicGrowth = new BasicGrowthComponent
            {
                WaterConsumption = component.WaterConsumption,
                NutrientConsumption = component.NutrientConsumption
            };
        }
        else
        {
            if (_random.Prob(0.5f))
                swabComp.WaterConsumption = component.WaterConsumption;
            if (_random.Prob(0.5f))
                swabComp.NutrientConsumption = component.NutrientConsumption;
        }
    }

    private void OnPlantGrow(Entity<BasicGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        if (!TryComp(uid, out PlantHolderComponent? holder)
            || !TryComp<PlantTraitsComponent>(uid, out var traits))
            return;

        if (holder.Seed == null || holder.Dead)
            return;

        // Check if the plant is viable.
        if (!traits.Viable)
        {
            holder.Health -= _random.Next(5, 10) * HydroponicsSpeedMultiplier;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;

            return;
        }

        // Advance plant age here.
        if (holder.SkipAging > 0)
        {
            holder.SkipAging--;
        }
        else
        {
            if (_random.Prob(0.8f))
                holder.Age += (int)(1 * HydroponicsSpeedMultiplier);

            holder.UpdateSpriteAfterUpdate = true;
        }

        if (holder.Age < 0) // Revert back to seed packet!
        {
            var packetSeed = holder.Seed;
            // will put it in the trays hands if it has any, please do not try doing this.
            _botany.SpawnSeedPacket(packetSeed, Transform(uid).Coordinates, uid);
            _plantHolder.RemovePlant(uid, holder);
            holder.ForceUpdate = true;
            _plantHolder.Update(uid, holder);
            return;
        }

        if (component.WaterConsumption > 0 && holder.WaterLevel > 0 && _random.Prob(0.75f))
        {
            holder.WaterLevel -= MathF.Max(0f,
                component.WaterConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);

            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        if (component.NutrientConsumption > 0 && holder.NutritionLevel > 0 && _random.Prob(0.75f))
        {
            holder.NutritionLevel -= MathF.Max(0f,
                component.NutrientConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);

            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        var healthMod = _random.Next(1, 3) * HydroponicsSpeedMultiplier;
        if (holder.SkipAging < 10)
        {
            // Make sure the plant is not thirsty.
            if (holder.WaterLevel > 10)
            {
                holder.Health += Convert.ToInt32(_random.Prob(0.35f)) * healthMod;
            }
            else
            {
                AffectGrowth((uid, holder), -1);
                holder.Health -= healthMod;
            }

            if (holder.NutritionLevel > 5)
            {
                holder.Health += Convert.ToInt32(_random.Prob(0.35f)) * healthMod;
            }
            else
            {
                AffectGrowth((uid, holder), -1);
                holder.Health -= healthMod;
            }

            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
    }

    /// <summary>
    /// Affects the growth of a plant by modifying its age or production timing.
    /// </summary>
    public void AffectGrowth(Entity<PlantHolderComponent> ent, int amount)
    {
        if(amount == 0)
            return;

        var (uid, component) = ent;

        if (component.Seed == null)
            return;

        if (!TryComp(uid, out PlantHarvestComponent? harvest)
            || !TryComp(uid, out PlantComponent? plant))
            return;

        if (amount > 0)
        {
            if (component.Age < plant.Maturation)
                component.Age += amount;
            else if (!harvest.ReadyForHarvest && plant.Yield <= 0f)
                harvest.LastHarvest -= amount;
        }
        else
        {
            if (component.Age < plant.Maturation)
                component.SkipAging++;
            else if (!harvest.ReadyForHarvest && plant.Yield <= 0f)
                harvest.LastHarvest += amount;
        }
    }
}

/// <summary>
/// Event of plant growing ticking.
/// </summary>
[ByRefEvent]
public readonly record struct OnPlantGrowEvent;
