using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Plant mutation entity effect that forces plant to exude gas while living.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateExudeGasesEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateExudeGases>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedConsumeExudeGasGrowthSystem _consumeExudeGasGrowth = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateExudeGases> args)
    {
        // No predict random.
        if (_net.IsClient)
            return;

        var amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        _consumeExudeGasGrowth.MutateRandomExudeGasses(entity.Owner, amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateExudeGases : EntityEffectBase<PlantMutateExudeGases>
{
    /// <summary>
    /// The minimum amount of gas to consume.
    /// </summary>
    [DataField]
    public float MinValue = 0.01f;

    /// <summary>
    /// The maximum amount of gas to consume.
    /// </summary>
    [DataField]
    public float MaxValue = 0.5f;

    /// <inheritdoc/>
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-plant-mutate-exude-gasses",
                ("chance", Probability),
                ("minValue", MinValue),
                ("maxValue", MaxValue));
    }
}

/// <summary>
/// Plant mutation entity effect that forces plant to consume gas while living.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateConsumeGasesEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateConsumeGases>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedConsumeExudeGasGrowthSystem _consumeExudeGasGrowth = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateConsumeGases> args)
    {
        var amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        _consumeExudeGasGrowth.MutateRandomConsumeGasses(entity.Owner, amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateConsumeGases : EntityEffectBase<PlantMutateConsumeGases>
{
    /// <summary>
    /// The minimum amount of gas to exude.
    /// </summary>
    [DataField]
    public float MinValue = 0.01f;

    /// <summary>
    /// The maximum amount of gas to exude.
    /// </summary>
    [DataField]
    public float MaxValue = 0.5f;

    /// <inheritdoc/>
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-plant-mutate-consume-gasses",
                ("chance", Probability),
                ("minValue", MinValue),
                ("maxValue", MaxValue));
    }
}
