using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Damage;

/// <summary>
/// Heals damage proportionally between a set of types. Total adjustment is modified by scale.
/// </summary>
/// <seealso cref="DamageableSystem.HealDistributed(Entity{DamageableComponent?}, FixedPoint2, ICollection{ProtoId{DamageTypePrototype}}?, EntityUid?)"/>
public sealed partial class EvenHealEntityEffectSystem : EntityEffectSystem<DamageableComponent, EvenHeal>
{
    [Dependency] private DamageableSystem _damageable = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<EvenHeal> args)
    {
        if (args.Effect.Heal <= 0)
            Log.Error($"Effect called by entity {args.User} with invalid heal amount {args.Effect.Heal}.");
        else
            _damageable.HealEvenly(entity.AsNullable(), -args.Effect.Heal * args.Scale, args.Effect.Types);
    }
}

/// <inheritdoc cref="EntityEffect"/>
/// <seealso cref="DamageableSystem.HealDistributed(Entity{DamageableComponent?}, FixedPoint2, ICollection{ProtoId{DamageTypePrototype}}?, EntityUid?)"/>
public sealed partial class EvenHeal : EntityEffectBase<EvenHeal>
{
    /// <summary>
    /// Damage types to heal.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<DamageTypePrototype>> Types;

    /// <summary>
    /// How much damage to heal. Value must be positive.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Heal;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Heal <= 0)
            return Loc.GetString("generic-invalid");

        var stringTypes = new List<string>();
        foreach (var type in Types)
        {
            stringTypes.Add(prototype.Index(type).LocalizedName);
        }

        return Loc.GetString("entity-effect-guidebook-even-heal",
            ("chance", Probability),
            ("amount", MathF.Abs(Heal.Float())),
            ("types", ContentLocalizationManager.FormatList(stringTypes)));
    }
}
