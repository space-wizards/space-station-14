using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class StartHeart : EntityEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-start-heart", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.System<HeartSystem>()
            .TryRestartHeart(args.TargetEntity);
    }
}
