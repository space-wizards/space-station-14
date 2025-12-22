using Content.Server.Botany.Components;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Plant mutation entity effect that forces plant to exude gas while living.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateExudeGasesEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateExudeGases>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateExudeGases> args)
    {
        var gasComponent = EnsureComp<ConsumeExudeGasGrowthComponent>(entity.Owner);
        var gasses = gasComponent.ExudeGasses;

        // Add a random amount of a random gas to this gas dictionary.
        var amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        var gas = _random.Pick(Enum.GetValues<Gas>());

        if (!gasses.TryAdd(gas, amount))
        {
            gasses[gas] += amount;
        }
    }
}

/// <summary>
/// Plant mutation entity effect that forces plant to consume gas while living.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateConsumeGasesEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateConsumeGases>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateConsumeGases> args)
    {
        var gasComponent = EnsureComp<ConsumeExudeGasGrowthComponent>(entity.Owner);
        var gasses = gasComponent.ConsumeGasses;

        // Add a random amount of a random gas to this gas dictionary.
        var amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        var gas = _random.Pick(Enum.GetValues<Gas>());

        if (!gasses.TryAdd(gas, amount))
        {
            gasses[gas] += amount;
        }
    }
}

