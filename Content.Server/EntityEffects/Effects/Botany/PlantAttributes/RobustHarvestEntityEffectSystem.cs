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
public sealed partial class RobustHarvestEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, RobustHarvest>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<RobustHarvest> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        if (entity.Comp.Seed.Potency < args.Effect.PotencyLimit)
        {
            entity.Comp.Seed.Potency = Math.Min(entity.Comp.Seed.Potency + args.Effect.PotencyIncrease, args.Effect.PotencyLimit);

            if (entity.Comp.Seed.Potency > args.Effect.PotencySeedlessThreshold)
            {
                entity.Comp.Seed.Seedless = true;
            }
        }
        else if (entity.Comp.Seed.Yield > 1 && _random.Prob(0.1f))
        {
            // Too much of a good thing reduces yield
            entity.Comp.Seed.Yield--;
        }
    }
}
