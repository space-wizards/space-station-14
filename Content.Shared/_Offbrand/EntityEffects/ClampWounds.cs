using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class ClampWounds : EntityEffect
{
    [DataField(required: true)]
    public float Chance;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-clamp-wounds", ("probability", Probability), ("chance", Chance));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.System<WoundableSystem>()
            .ClampWounds(args.TargetEntity, Chance);
    }
}
