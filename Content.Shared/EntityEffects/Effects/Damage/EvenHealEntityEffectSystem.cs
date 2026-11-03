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

public sealed partial class EvenHeal : EntityEffectBase<EvenHeal>
{
    /// <summary>
    /// How much damage to heal. Value must be positive.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Heal;

    /// <summary>
    /// Damage types to heal.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<DamageTypePrototype>>? Types;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Heal <= 0)
            return Loc.GetString("generic-invalid"); // Make it super obvious to use a positive value

        var stringTypes = "";
        if (Types != null)
        {
            var typeList = new List<string>();
            foreach (var type in Types)
            {
                typeList.Add(prototype.Index(type).LocalizedName);
            }

            stringTypes = ContentLocalizationManager.FormatList(typeList);
        }

        return Loc.GetString("entity-effect-guidebook-even-heal",
            ("chance", Probability),
            ("amount", Heal.Float()),
            ("typeCount", Types?.Count ?? 0),
            ("types", stringTypes));
    }
}
