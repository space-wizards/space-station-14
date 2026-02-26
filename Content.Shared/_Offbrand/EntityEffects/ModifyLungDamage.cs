using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class ModifyLungDamage : EntityEffectBase<ModifyLungDamage>
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Amount < FixedPoint2.Zero)
            return Loc.GetString("entity-effect-guidebook-modify-lung-damage-heals", ("chance", Probability), ("amount", -Amount));
        else
            return Loc.GetString("entity-effect-guidebook-modify-lung-damage-deals", ("chance", Probability), ("amount", Amount));
    }
}

public sealed class ModifyLungDamageEntityEffectSystem : EntityEffectSystem<LungDamageComponent, ModifyLungDamage>
{
    [Dependency] private readonly LungDamageSystem _lungDamage = default!;

    protected override void Effect(Entity<LungDamageComponent> ent, ref EntityEffectEvent<ModifyLungDamage> args)
    {
        _lungDamage.TryModifyDamage(ent.AsNullable(), args.Effect.Amount * args.Scale);
    }
}
