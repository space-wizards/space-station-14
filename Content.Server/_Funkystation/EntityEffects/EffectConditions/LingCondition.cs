using Content.Shared.Changeling;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._Funkystation.EntityEffects.EffectConditions;

public sealed partial class LingCondition : EntityEffectCondition
{
    public override bool Condition(EntityEffectBaseArgs args)
    {
        return args.EntityManager.HasComponent<ChangelingComponent>(args.TargetEntity);
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-ling");
    }
}