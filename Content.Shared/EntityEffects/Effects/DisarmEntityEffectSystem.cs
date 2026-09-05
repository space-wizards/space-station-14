using Content.Shared.CombatMode;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Disarms the entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class DisarmEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Disarm>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Disarm> args)
    {
        var dis = new DisarmedEvent(entity.Owner, entity.Owner, 0f);
        RaiseLocalEvent(entity.Owner, ref dis);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Disarm : EntityEffectBase<Disarm>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("entity-effect-guidebook-disarm", ("chance", Probability));
}
