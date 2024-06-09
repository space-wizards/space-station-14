using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffectConditions;

public sealed partial class Satiation : ReagentEffectCondition
{
    [DataField]
    public float Max = float.PositiveInfinity;

    [DataField]
    public float Min = 0;

    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType = "Hunger";

    [Dependency] private readonly SatiationSystem _satiation = default!;

    public override bool Condition(ReagentEffectArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out SatiationComponent? component))
            return false;

        if (!_satiation.TryGetCurrentSatiation((args.SolutionEntity, component), SatiationType, out var total))
            return false;

        return total > Min && total < Max;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-total-satiation",
            ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
            ("min", Min),
            ("type", Loc.GetString(prototype.Index(SatiationType).Name)));
    }
}
