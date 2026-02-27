using Content.Shared.Atmos.Rotting;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Reduces the rotting timer on an entity by a number of seconds, modified by scale.
/// This cannot increase the amount of seconds a body has rotted.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ReduceRottingEntityEffectSystem : EntityEffectSystem<PerishableComponent, ReduceRotting>
{
    [Dependency] private readonly SharedRottingSystem _rotting = default!;

    protected override void Effect(Entity<PerishableComponent> entity, ref EntityEffectEvent<ReduceRotting> args)
    {
        var amount = args.Effect.Seconds * args.Scale;

        _rotting.ReduceAccumulator(entity, amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ReduceRotting : EntityEffectBase<ReduceRotting>
{
    /// <summary>
    /// Number of seconds removed from the rotting timer.
    /// </summary>
    [DataField]
    public TimeSpan Seconds = TimeSpan.FromSeconds(10);

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-reduce-rotting",
            ("chance", Probability),
            ("time", Seconds.TotalSeconds));
}
