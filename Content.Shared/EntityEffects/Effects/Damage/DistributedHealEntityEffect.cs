using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Damage;

/// <summary>
/// Evenly heal the damage in a set of types. Total adjustment is modified by scale.
/// </summary>
/// <seealso cref="DamageableSystem.HealEvenly(Entity{DamageableComponent?}, FixedPoint2, ICollection{ProtoId{DamageTypePrototype}}?, EntityUid?)"/>
public sealed partial class DistributedHealEntityEffectSystem : EntityEffectSystem<DamageableComponent, DistributedHeal>
{
    [Dependency] private DamageableSystem _damageable = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<DistributedHeal> args)
    {
        if (args.Effect.Heal <= 0)
            Log.Error($"Effect called by entity {args.User} with invalid heal amount {args.Effect.Heal}.");
        else
            _damageable.HealDistributed(entity.AsNullable(), -args.Effect.Heal * args.Scale, args.Effect.Types);
    }
}

/// <inheritdoc cref="EntityEffect"/>
/// <seealso cref="DamageableSystem.HealEvenly(Entity{DamageableComponent?}, FixedPoint2, ICollection{ProtoId{DamageTypePrototype}}?, EntityUid?)"/>
public sealed partial class DistributedHeal : EntityEffectBase<DistributedHeal>
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

        return Loc.GetString("entity-effect-guidebook-distributed-heal",
            ("chance", Probability),
            ("amount", MathF.Abs(Heal.Float())),
            ("types", ContentLocalizationManager.FormatList(stringTypes)));
    }
}
