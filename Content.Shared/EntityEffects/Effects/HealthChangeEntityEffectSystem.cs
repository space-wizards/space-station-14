using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// This is used for...
/// </summary>
public sealed partial class HealthChangeEntityEffectSystem : EntityEffectSystem<DamageableComponent, HealthChange>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<HealthChange> args)
    {
        var scale = FixedPoint2.New(1);
        var damageSpec = new DamageSpecifier(args.Effect.Damage);

        // TODO: Reagents need to determine scale by quantity and modify the scale value BEFORE starting the entity effect.
        damageSpec *= args.Effect.ScaleByQuantity ? args.Scale : scale;

        if (!MathHelper.CloseTo(_damageable.UniversalReagentDamageModifier, 1f) || !MathHelper.CloseTo(_damageable.UniversalReagentHealModifier, 1f))
        {
            foreach (var (type, val) in damageSpec.DamageDict)
            {
                if (val < 0f)
                {
                    damageSpec.DamageDict[type] = val * _damageable.UniversalReagentHealModifier;
                }
                if (val > 0f)
                {
                    damageSpec.DamageDict[type] = val * _damageable.UniversalReagentDamageModifier;
                }
            }
        }

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
