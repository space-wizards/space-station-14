using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// This effect directly increases the potency of a PlantHolder's plant provided it exists and isn't dead.
/// Potency directly correlates to the size of the plant's produce.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class RobustHarvestEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, RobustHarvest>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<RobustHarvest> args)
    {
        if (entity.Comp.PlantEntity == null || Deleted(entity.Comp.PlantEntity))
            return;

        var plantUid = entity.Comp.PlantEntity.Value;
        if (!TryComp<PlantComponent>(plantUid, out var plant) || !TryComp<PlantTraitsComponent>(plantUid, out var traits) || !TryComp<PlantHolderComponent>(plantUid, out var plantHolder) || plantHolder.Dead)
            return;

        if (plant.Potency < args.Effect.PotencyLimit)
        {
            plant.Potency = Math.Min(plant.Potency + args.Effect.PotencyIncrease, args.Effect.PotencyLimit);

            if (plant.Potency > args.Effect.PotencySeedlessThreshold)
                traits.Seedless = true;
        }
        else if (plant.Yield > 1 && _random.Prob(0.1f))
        {
            // Too much of a good thing reduces yield.
            plant.Yield--;
        }
    }
}
