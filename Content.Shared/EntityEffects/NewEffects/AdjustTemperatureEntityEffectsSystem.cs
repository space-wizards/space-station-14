using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Shared.EntityEffects.NewEffects;

public sealed partial class AdjustTemperatureEntityEffectSystem : EntityEffectSystem<TemperatureComponent, AdjustTemperature>
{
    [Dependency] private readonly SharedTemperatureSystem _temperature = default!;
    protected override void Effect(Entity<TemperatureComponent> entity, ref EntityEffectEvent<AdjustTemperature> args)
    {
        var amount = args.Effect.Amount * args.Scale;

        _temperature.ChangeHeat(entity, amount, true, entity.Comp);
    }
}

public sealed partial class AdjustTemperature : EntityEffectBase<AdjustTemperature>
{
    /// <summary>
    ///     Amount of firestacks reduced.
    /// </summary>
    [DataField]
    public float Amount;
}
