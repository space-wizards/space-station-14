using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class EvenHealthChangeEntityEffectSystem : EntityEffectSystem<DamageableComponent, EvenHealthChange>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<EvenHealthChange> args)
    {
        var universalReagentDamageModifier = _damageable.UniversalReagentDamageModifier;
        var universalReagentHealModifier = _damageable.UniversalReagentHealModifier;

        var dspec = new DamageSpecifier();

        foreach (var (group, amount) in args.Effect.Damage)
        {
            var groupProto = _proto.Index(group);
            var groupDamage = new Dictionary<string, FixedPoint2>();
            foreach (var damageId in groupProto.DamageTypes)
            {
                var damageAmount = entity.Comp.Damage.DamageDict.GetValueOrDefault(damageId);
                if (damageAmount != FixedPoint2.Zero)
                    groupDamage.Add(damageId, damageAmount);
            }

            var sum = groupDamage.Values.Sum();
            foreach (var (damageId, damageAmount) in groupDamage)
            {
                var existing = dspec.DamageDict.GetOrNew(damageId);
                dspec.DamageDict[damageId] = existing + damageAmount / sum * amount;
            }
        }

        if (MathHelper.CloseTo(universalReagentDamageModifier, 1f) || MathHelper.CloseTo(universalReagentHealModifier, 1f))
        {
            foreach (var (type, val) in dspec.DamageDict)
            {
                if (val < 0f)
                {
                    dspec.DamageDict[type] = val * universalReagentHealModifier;
                }
                if (val > 0f)
                {
                    dspec.DamageDict[type] = val * universalReagentDamageModifier;
                }
            }
        }

        _damageable.TryChangeDamage(
            entity,
            dspec * args.Scale,
            args.Effect.IgnoreResistances,
            interruptsDoAfters: false,
            damageable: entity.Comp);
    }
}

public sealed partial class EvenHealthChange : EntityEffectBase<EvenHealthChange>
{
    /// <summary>
    /// Damage to heal, collected into entire damage groups.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> Damage = new();

    /// <summary>
    /// Should this effect scale the damage by the amount of chemical in the solution?
    /// Useful for touch reactions, like styptic powder or acid.
    /// Only usable if the EntityEffectBaseArgs is an EntityEffectReagentArgs.
    /// </summary>
    [DataField]
    public bool ScaleByQuantity;

    /// <summary>
    /// Should this effect ignore damage modifiers?
    /// </summary>
    [DataField]
    public bool IgnoreResistances = true;
}
