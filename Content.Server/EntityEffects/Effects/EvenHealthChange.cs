using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Version of <see cref="HealthChange"/> that distributes the healing to groups
/// </summary>
[UsedImplicitly]
public sealed partial class EvenHealthChange : EntityEffect
{
    /// <summary>
    /// damage to heal, collected into entire damage groups
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> Damage;

    /// <summary>
    ///     Should this effect scale the damage by the amount of chemical in the solution?
    ///     Useful for touch reactions, like styptic powder or acid.
    ///     Only usable if the EntityEffectBaseArgs is an EntityEffectReagentArgs.
    /// </summary>
    [DataField]
    public bool ScaleByQuantity;

    [DataField]
    public bool IgnoreResistances = true;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var damages = new List<string>();
        var heals = false;
        var deals = false;

        var universalReagentDamageModifier = entSys.GetEntitySystem<DamageableSystem>().UniversalReagentDamageModifier;
        var universalReagentHealModifier = entSys.GetEntitySystem<DamageableSystem>().UniversalReagentHealModifier;

        foreach (var (group, amount) in Damage)
        {
            var groupProto = prototype.Index(group);

            var sign = FixedPoint2.Sign(amount);
            var mod = 1f;

            if (sign < 0)
            {
                heals = true;
                mod = universalReagentHealModifier;
            }
            else if (sign > 0)
            {
                deals = true;
                mod = universalReagentDamageModifier;
            }

            damages.Add(
                Loc.GetString("health-change-display",
                    ("kind", groupProto.LocalizedName),
                    ("amount", MathF.Abs(amount.Float() * mod)),
                    ("deltasign", sign)
                ));
        }

        var healsordeals = heals ? deals ? "both" : "heals" : deals ? "deals" : "none";
        return Loc.GetString("reagent-effect-guidebook-even-health-change",
            ("chance", Probability),
            ("changes", ContentLocalizationManager.FormatList(damages)),
            ("healsordeals", healsordeals));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent<DamageableComponent>(args.TargetEntity, out var damageable))
            return;
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        var scale = FixedPoint2.New(1);

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            scale = ScaleByQuantity ? reagentArgs.Quantity * reagentArgs.Scale : reagentArgs.Scale;
        }

        var universalReagentDamageModifier = args.EntityManager.System<DamageableSystem>().UniversalReagentDamageModifier;
        var universalReagentHealModifier = args.EntityManager.System<DamageableSystem>().UniversalReagentHealModifier;

        var dspec = new DamageSpecifier();

        foreach (var (group, amount) in Damage)
        {
            var groupProto = protoMan.Index(group);
            var groupDamage = new Dictionary<string, FixedPoint2>();
            foreach (var damageId in groupProto.DamageTypes)
            {
                var damageAmount = damageable.Damage.DamageDict.GetValueOrDefault(damageId);
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

        if (universalReagentDamageModifier != 1 || universalReagentHealModifier != 1)
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

        args.EntityManager.System<DamageableSystem>()
            .TryChangeDamage(
                args.TargetEntity,
                dspec * scale,
                IgnoreResistances,
                interruptsDoAfters: false);
    }
}
