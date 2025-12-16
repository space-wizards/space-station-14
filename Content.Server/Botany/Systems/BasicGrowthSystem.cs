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
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

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
        var (plantUid, component) = ent;
        var (_, tray) = args.Tray;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        // Check if the plant is viable.
        if (TryComp<PlantTraitsComponent>(plantUid, out var traits) && !traits.Viable)
        {
            holder.Health -= _random.Next(5, 10) * tray.TraySpeedMultiplier;
            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;

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
                holder.Age += (int)(1 * tray.TraySpeedMultiplier);

            tray.UpdateSpriteAfterUpdate = true;
        }

        if (holder.Age < 0) // Revert back to seed packet!
        {
            // will put it in the trays hands if it has any, please do not try doing this.
            _botany.SpawnSeedPacketFromPlant(plantUid, Transform(args.Tray.Owner).Coordinates, args.Tray.Owner);
            _plantTray.RemovePlant(args.Tray.AsNullable());
            tray.ForceUpdate = true;
            _plantTray.Update(args.Tray.AsNullable());
            return;
        }

        if (component.WaterConsumption > 0 && tray.WaterLevel > 0 && _random.Prob(0.75f))
        {
            tray.WaterLevel -= MathF.Max(0f,
                component.WaterConsumption * tray.TrayConsumptionMultiplier * tray.TraySpeedMultiplier);

            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }

        if (component.NutrientConsumption > 0 && tray.NutritionLevel > 0 && _random.Prob(0.75f))
        {
            tray.NutritionLevel -= MathF.Max(0f,
                component.NutrientConsumption * tray.TrayConsumptionMultiplier * tray.TraySpeedMultiplier);

            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }

        var healthMod = _random.Next(1, 3) * tray.TraySpeedMultiplier;
        if (holder.SkipAging < 10)
        {
            // Make sure the plant is not thirsty.
            if (tray.WaterLevel > 10)
            {
                holder.Health += Convert.ToInt32(_random.Prob(0.35f)) * healthMod;
            }
            else
            {
                AffectGrowth((plantUid, holder), -1);
                holder.Health -= healthMod;
            }

            if (tray.NutritionLevel > 5)
            {
                holder.Health += Convert.ToInt32(_random.Prob(0.35f)) * healthMod;
            }
            else
            {
                AffectGrowth((plantUid, holder), -1);
                holder.Health -= healthMod;
            }

            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }
    }

    /// <summary>
    /// Affects the growth of a plant by modifying its age or production timing.
    /// </summary>
    [PublicAPI]
    public void AffectGrowth(Entity<PlantHolderComponent?> ent, int amount)
    {
        if (amount == 0)
            return;

        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (!TryComp<PlantHarvestComponent>(uid, out var harvest)
            || !TryComp<PlantComponent>(uid, out var plant))
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
public readonly record struct OnPlantGrowEvent(Entity<PlantTrayComponent> Tray);
