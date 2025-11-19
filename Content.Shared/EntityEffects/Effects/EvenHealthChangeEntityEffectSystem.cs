using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Evenly adjust the damage types in a damage group by up to a specified total on this entity.
/// Total adjustment is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class EvenHealthChangeEntityEffectSystem : EntityEffectSystem<DamageableComponent, EvenHealthChange>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<EvenHealthChange> args)
    {
        var damageSpec = new DamageSpecifier();

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
                var existing = damageSpec.DamageDict.GetOrNew(damageId);
                damageSpec.DamageDict[damageId] = existing + damageAmount / sum * amount;
            }
        }

        damageSpec *= args.Scale;

        _damageable.TryChangeDamage(
            entity.AsNullable(),
            damageSpec,
            args.Effect.IgnoreResistances,
            interruptsDoAfters: false);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class EvenHealthChange : EntityEffectBase<EvenHealthChange>
{
    /// <summary>
    /// Damage to heal, collected into entire damage groups.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> Damage = new();

    /// <summary>
    /// Should this effect ignore damage modifiers?
    /// </summary>
    [DataField]
    public bool IgnoreResistances = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var damages = new List<string>();
        var heals = false;
        var deals = false;

        var damagableSystem = entSys.GetEntitySystem<DamageableSystem>();
        var universalReagentDamageModifier = damagableSystem.UniversalReagentDamageModifier;
        var universalReagentHealModifier = damagableSystem.UniversalReagentHealModifier;

        foreach (var (group, amount) in Damage)
        {
            var groupProto = prototype.Index(group);

            var sign = FixedPoint2.Sign(amount);
            float mod;

            switch (sign)
            {
                case < 0:
                    heals = true;
                    mod = universalReagentHealModifier;
                    break;
                case > 0:
                    deals = true;
                    mod = universalReagentDamageModifier;
                    break;
                default:
                    continue; // Don't need to show damage types of 0...
            }

            damages.Add(
                Loc.GetString("health-change-display",
                    ("kind", groupProto.LocalizedName),
                    ("amount", MathF.Abs(amount.Float() * mod)),
                    ("deltasign", sign)
                ));
        }

        var healsordeals = heals ? deals ? "both" : "heals" : deals ? "deals" : "none";
        return Loc.GetString("entity-effect-guidebook-even-health-change",
            ("chance", Probability),
            ("changes", ContentLocalizationManager.FormatList(damages)),
            ("healsordeals", healsordeals));
    }
}
