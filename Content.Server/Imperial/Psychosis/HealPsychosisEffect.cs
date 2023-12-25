using Content.Shared.Chemistry.Reagent;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class HealPsychosis : ReagentEffect
{
    [DataField("reagent")]
    public string Reagent = "";

    [DataField("stage")]
    public int Stage = 0;
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;
        if (!entityManager.TryGetComponent<PsychosisComponent>(uid, out var psychosis))
            return;
        if (!(psychosis.Stage <= Stage))
            return;
        if (Stage == 3)
        {
            if (psychosis.HealThird == Reagent)
            {
                entityManager.RemoveComponent<PsychosisComponent>(uid);
            }
        }
        if (Stage == 2)
        {
            if (psychosis.HealSecond == Reagent)
            {
                entityManager.RemoveComponent<PsychosisComponent>(uid);
            }
        }
        if (Stage == 1)
        {
            if (psychosis.HealFirst == Reagent)
            {
                entityManager.RemoveComponent<PsychosisComponent>(uid);
            }
        }
    }
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Stage == 1)
            return Loc.GetString("psychosis-first-stage-heal-effect");
        if (Stage == 2)
            return Loc.GetString("psychosis-second-stage-heal-effect");
        if (Stage == 3)
            return Loc.GetString("psychosis-third-stage-heal-effect");
        throw new NotImplementedException();
    }
}
