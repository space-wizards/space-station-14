using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class ModifyLungDamage : EntityEffect
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Amount < FixedPoint2.Zero)
            return Loc.GetString("reagent-effect-guidebook-modify-lung-damage-heals", ("chance", Probability), ("amount", -Amount));
        else
            return Loc.GetString("reagent-effect-guidebook-modify-lung-damage-deals", ("chance", Probability), ("amount", Amount));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var scale = FixedPoint2.New(1);

        if (args is EntityEffectReagentArgs reagentArgs)
            scale = reagentArgs.Scale;

        args.EntityManager.System<LungDamageSystem>()
            .TryModifyDamage(args.TargetEntity, Amount * scale);
    }
}
