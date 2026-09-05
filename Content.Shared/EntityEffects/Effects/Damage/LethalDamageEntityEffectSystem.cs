using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Suicide;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Damage;

/// <summary>
/// Applies damage through the suicide system to reach the highest mob threshold.
/// Has no effect on entities without mob thresholds.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class LethalDamageEntityEffectSystem : EntityEffectSystem<DamageableComponent, LethalDamage>
{
    [Dependency] private SharedSuicideSystem _suicide = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<LethalDamage> args)
    {
        _suicide.ApplyLethalDamage(entity, args.Effect.DamageType);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class LethalDamage : EntityEffectBase<LethalDamage>
{
    [DataField(required: true)]
    public ProtoId<DamageTypePrototype> DamageType;
}
