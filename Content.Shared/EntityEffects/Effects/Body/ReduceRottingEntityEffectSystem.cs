using Content.Shared.Atmos.Rotting;

namespace Content.Shared.EntityEffects.Effects.Body;

public sealed partial class ReduceRottingEntityEffectSystem : EntityEffectSystem<PerishableComponent, ReduceRotting>
{
    [Dependency] private readonly SharedRottingSystem _rotting = default!;

    protected override void Effect(Entity<PerishableComponent> entity, ref EntityEffectEvent<ReduceRotting> args)
    {
        var amount = args.Effect.Seconds *= args.Scale;

        _rotting.ReduceAccumulator(entity, amount);
    }
}

public sealed partial class ReduceRotting : EntityEffectBase<ReduceRotting>
{
    [DataField]
    public TimeSpan Seconds = TimeSpan.FromSeconds(10);
}
