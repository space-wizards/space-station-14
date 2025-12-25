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
public sealed partial class RobustHarvestEntityEffectSystem : EntityEffectSystem<PlantComponent, RobustHarvest>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<RobustHarvest> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        if (!TryComp<PlantComponent>(entity, out var plant)
            || !TryComp<PlantTraitsComponent>(entity, out var traits))
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
