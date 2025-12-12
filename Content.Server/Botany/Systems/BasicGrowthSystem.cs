using Content.Server.Botany.Components;
using Content.Shared.Swab;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

// TODO: make CO2Boost (add potency if the plant can eat an increasing amount of CO2). separate PR post-merge
// TODO: make GrowLight (run bonus ticks if theres a grow light nearby). separate PR post-merge.
/// <summary>
/// Handles baseline plant progression each growth tick: aging, resource consumption,
/// simple viability checks, and basic swab cross-pollination behavior.
/// </summary>
public sealed class BasicGrowthSystem : PlantGrowthSystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

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

        PlantHolderComponent? holder = null;
        Resolve(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead)
            return;

        // Check if the plant is viable
        if (TryComp<PlantTraitsComponent>(uid, out var traits) && !traits.Viable)
        {
            holder.Health -= _random.Next(5, 10) * HydroponicsSpeedMultiplier;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;

            return;
        }

        // Advance plant age here.
        if (holder.SkipAging > 0)
            holder.SkipAging--;
        else
        {
            if (_random.Prob(0.8f))
            {
                holder.Age += (int)(1 * HydroponicsSpeedMultiplier);
                holder.UpdateSpriteAfterUpdate = true;
            }
        }

        if (holder.Age < 0) // Revert back to seed packet!
        {
            var packetSeed = holder.Seed;
            // will put it in the trays hands if it has any, please do not try doing this
            _botany.SpawnSeedPacket(packetSeed, Transform(uid).Coordinates, uid);
            _plantHolder.RemovePlant(uid, holder);
            holder.ForceUpdate = true;
            _plantHolder.Update(uid, holder);
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
        }
    }
}
