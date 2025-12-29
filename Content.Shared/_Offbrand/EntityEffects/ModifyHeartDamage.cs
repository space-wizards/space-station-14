using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class ModifyHeartDamage : EntityEffectBase<ModifyHeartDamage>
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Amount < FixedPoint2.Zero)
            return Loc.GetString("entity-effect-guidebook-modify-heart-damage-heals", ("chance", Probability), ("amount", -Amount));
        else
            return Loc.GetString("entity-effect-guidebook-modify-heart-damage-deals", ("chance", Probability), ("amount", Amount));
    }
}

public sealed class ModifyHeartDamageEntityEffectSystem : EntityEffectSystem<HeartrateComponent, ModifyHeartDamage>
{
    [Dependency] private readonly HeartSystem _heart = default!;

    protected override void Effect(Entity<HeartrateComponent> ent, ref EntityEffectEvent<ModifyHeartDamage> args)
    {
        _heart.ChangeHeartDamage(ent.AsNullable(), args.Effect.Amount * args.Scale);
    }
}
