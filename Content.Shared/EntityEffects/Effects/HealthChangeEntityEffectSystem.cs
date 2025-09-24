using Content.Shared.Damage;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// This is used for...
/// </summary>
public sealed partial class HealthChangeEntityEffectSystem : EntityEffectSystem<DamageableComponent, HealthChange>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<HealthChange> args)
    {
        var damageSpec = new DamageSpecifier(args.Effect.Damage);

        // TODO: This incorrectly scaled parabolically before, so we need adjust every effect that has this set to true.
        damageSpec *= args.Effect.ScaleByQuantity ? args.Scale : float.Min(1f, args.Scale);

        _damageable.TryChangeDamage(
                entity,
                damageSpec,
                args.Effect.IgnoreResistances,
                interruptsDoAfters: false);
    }
}

public sealed partial class HealthChange : EntityEffectBase<HealthChange>
{
    /// <summary>
    /// Damage to apply every cycle. Damage Ignores resistances.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    ///     Should this effect scale the damage by the amount of chemical in the solution?
    ///     Useful for touch reactions, like styptic powder or acid.
    ///     Only usable if the EntityEffectBaseArgs is an EntityEffectReagentArgs.
    /// </summary>
    [DataField]
    public bool ScaleByQuantity;

    [DataField]
    public bool IgnoreResistances = true;
}
