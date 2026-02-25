using Content.Shared._Offbrand.EntityEffects;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Server._Offbrand.EntityEffects;

public sealed class MetaboliteThresholdEntityConditionSystem : EntityConditionSystem<MetabolizerComponent, MetaboliteThresholdCondition>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;

    private static readonly ProtoId<MetabolismStagePrototype> MetabolitesStage = "Metabolites";
    private static readonly ProtoId<MetabolismStagePrototype> BloodstreamStage = "Bloodstream";

    protected override void Condition(Entity<MetabolizerComponent> ent, ref EntityConditionEvent<MetaboliteThresholdCondition> args)
    {
        var solutions = ent.Comp.Solutions;
        if (!solutions.TryGetValue(MetabolitesStage, out var stage))
            return;

        if (!_metabolizer.LookupSolution(ent, stage, false, out var metabolitesSolution, out _, out _))
            return;

        var quant = metabolitesSolution.GetTotalPrototypeQuantity(args.Condition.Reagent);

        if (args.Condition.IncludeBloodstream && solutions.TryGetValue(BloodstreamStage, out var bloodstreamStage) && _metabolizer.LookupSolution(ent, bloodstreamStage, false, out var solution, out _, out _))
        {
            quant += solution.GetTotalPrototypeQuantity(args.Condition.Reagent);
        }

        args.Result = quant >= args.Condition.Min && quant <= args.Condition.Max;
    }
}
