using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Plant mutation entity effect that forces plant to exude gas while living.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateExudeGasesEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantMutateExudeGases>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantMutateExudeGases> args)
    {
        if (!_plantTray.HasPlant(entity.AsNullable()))
            return;

        var gasComponent = EnsureComp<ConsumeExudeGasGrowthComponent>(entity.Comp.PlantEntity!.Value);
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
public sealed partial class PlantMutateConsumeGasesEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantMutateConsumeGases>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantMutateConsumeGases> args)
    {
        if (!_plantTray.HasPlant(entity.AsNullable()))
            return;

        var gasComponent = EnsureComp<ConsumeExudeGasGrowthComponent>(entity.Comp.PlantEntity!.Value);
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

