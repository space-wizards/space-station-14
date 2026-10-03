using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IReagentCondition : ICondition<IReagentCondition>, IConditionWithDefaultSatisfactionRule
{
    /// <summary>
    /// Minimum amount required.
    /// </summary>
    FixedPoint2 Min { get; }

    /// <summary>
    /// Maximum amount required
    /// </summary>
    FixedPoint2 Max { get; }

    /// <summary>
    /// Reagent we look for in a solution component.
    /// </summary>
    ProtoId<ReagentPrototype> Reagent { get; }

    /// <summary>
    /// The solution to look for. if null we take the solutionComponent of the Entity.
    /// </summary>
    string? Solution { get; }

    /// <inheritdoc />
    Satisfier.Satisfier IConditionWithDefaultSatisfactionRule.GetDefaultSatisfier()
    {
        if (Min == Max)
        {
            return new WithThreshold()
            {
                Threshold = 1,
                Comparison = WithThreshold.Comparator.Equal
            };
        }

        return new WithBoundary
        {
            IncludeLowerBound = true,
            IncludeUpperBound = true,
            LowerBound = 0,
            UpperBound = 1,
            Inverted = false,
        };
    }
}

/// <summary>
/// Returns true if this solution entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
public sealed partial class ReagentEntityConditionSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MetaDataComponent> entity, ref ConditionEvaluationEvent<IReagentCondition> args)
    {
        args.Handled = true;

        if (args.Condition.Max == args.Condition.Min && args.Condition.Max == 0)
            args.Value = 1;

        Solution? solution;
        if (args.Condition.Solution == null)
        {
            if (!TryComp(entity.Owner, out SolutionComponent? solutionComponent))
                return;
            solution = solutionComponent.Solution;
        }
        else
        {
            if (!_solutionContainerSystem.TryGetSolution(entity.Owner, args.Condition.Solution, out _, out solution))
                return;
        }

        var quant = solution.GetTotalPrototypeQuantity(args.Condition.Reagent);

        if (args.Condition.Max == args.Condition.Min)
        {
            args.Value = quant == args.Condition.Max ? 1 : 0;
        }
        else
        {
            args.Value = ((quant - args.Condition.Min) / (args.Condition.Max - args.Condition.Min)).Float();
        }
    }
}
