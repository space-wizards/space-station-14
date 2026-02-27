using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Shared.EntityEffects.Effects;

// TODO: When we get a proper temperature/energy struct combine this with the solution temperature effect!!!
/// <summary>
/// Adjusts the temperature of this entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AdjustTemperatureEntityEffectSystem : EntityEffectSystem<TemperatureComponent, AdjustTemperature>
{
    [Dependency] private readonly SharedTemperatureSystem _temperature = default!;
    protected override void Effect(Entity<TemperatureComponent> entity, ref EntityEffectEvent<AdjustTemperature> args)
    {
        var amount = args.Effect.Amount * args.Scale;

        _temperature.ChangeHeat(entity, amount, true, entity.Comp);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdjustTemperature : EntityEffectBase<AdjustTemperature>
{
    /// <summary>
    ///     Amount we're adjusting temperature by.
    /// </summary>
    [DataField]
    public float Amount;
}
