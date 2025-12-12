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

        if (!TryComp<PlantTraitsComponent>(entity, out var traits))
            return;

        if (traits.Potency < args.Effect.PotencyLimit)
        {
            traits.Potency = Math.Min(traits.Potency + args.Effect.PotencyIncrease, args.Effect.PotencyLimit);

            if (traits.Potency > args.Effect.PotencySeedlessThreshold)
            {
                traits.Seedless = true;
            }
        }
        else if (traits.Yield > 1 && _random.Prob(0.1f))
        {
            // Too much of a good thing reduces yield
            traits.Yield--;
        }
    }
}
