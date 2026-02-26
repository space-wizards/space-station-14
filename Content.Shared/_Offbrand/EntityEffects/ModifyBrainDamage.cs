using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class ModifyBrainDamage : EntityEffectBase<ModifyBrainDamage>
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Amount < FixedPoint2.Zero)
            return Loc.GetString("entity-effect-guidebook-modify-brain-damage-heals", ("chance", Probability), ("amount", -Amount));
        else
            return Loc.GetString("entity-effect-guidebook-modify-brain-damage-deals", ("chance", Probability), ("amount", Amount));
    }
}

public sealed class ModifyBrainDamageEntityEffectSystem : EntityEffectSystem<BrainDamageComponent, ModifyBrainDamage>
{
    [Dependency] private readonly BrainDamageSystem _brainDamage = default!;

    protected override void Effect(Entity<BrainDamageComponent> ent, ref EntityEffectEvent<ModifyBrainDamage> args)
    {
        _brainDamage.TryChangeBrainDamage(ent.AsNullable(), args.Effect.Amount * args.Scale);
    }
}
