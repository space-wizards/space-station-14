using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
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
    [Dependency] private readonly ConsumeExudeGasGrowthSystem _consumeExudeGasGrowth = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateExudeGases> args)
    {
        var amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        _consumeExudeGasGrowth.MutateRandomExudeGasses(entity.Owner, amount);
    }
}

/// <summary>
/// Plant mutation entity effect that forces plant to consume gas while living.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateConsumeGasesEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateConsumeGases>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ConsumeExudeGasGrowthSystem _consumeExudeGasGrowth = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateConsumeGases> args)
    {
        var amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        _consumeExudeGasGrowth.MutateRandomConsumeGasses(entity.Owner, amount);
    }
}

