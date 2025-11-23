using Content.Server.Body.Components;
using Content.Shared._Offbrand.EntityEffects;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;

namespace Content.Server._Offbrand.EntityEffects;

public sealed class MetaboliteThresholdEntityConditionSystem : EntityConditionSystem<MetabolizerComponent, MetaboliteThresholdCondition>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private Solution? GetSolution(Entity<MetabolizerComponent, OrganComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return null;

        if (ent.Comp1.SolutionOnBody)
        {
            if (ent.Comp2.Body is { } body && _solutionContainer.TryGetSolution(body, ent.Comp1.SolutionName, out _, out var solution))
                return solution;

            return null;
        }
        else
        {
            if (_solutionContainer.TryGetSolution(ent.Owner, ent.Comp1.SolutionName, out _, out var solution))
                return solution;
        }

        return null;
    }

    protected override void Condition(Entity<MetabolizerComponent> ent, ref EntityConditionEvent<MetaboliteThresholdCondition> args)
    {
        var reagent = args.Condition.Reagent;
        var metabolites = ent.Comp.Metabolites;

        var quant = FixedPoint2.Zero;
        metabolites.TryGetValue(reagent, out quant);

        if (args.Condition.IncludeBloodstream && GetSolution((ent, ent.Comp, null)) is { } solution)
        {
            quant += solution.GetTotalPrototypeQuantity(reagent);
        }

        args.Result = quant >= args.Condition.Min && quant <= args.Condition.Max;
    }
}
