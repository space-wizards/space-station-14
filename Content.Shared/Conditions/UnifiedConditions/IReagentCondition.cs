using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IReagentCondition : ICondition, IConditionWithDefaultSatisfactionRule
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
            return new WithThreshold
            {
                Threshold = 1,
                Comparison = WithThreshold.Comparator.Equal,
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
public sealed partial class ReagentEntityConditionSystem : ConditionEvaluatorSystem<IReagentCondition>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override float Evaluate(IReagentCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (condition.Max == condition.Min && condition.Max == 0)
            return 1;

        Solution? solution;
        if (condition.Solution == null)
        {
            if (!TryComp(entityUid, out SolutionComponent? solutionComponent))
                return 0;
            solution = solutionComponent.Solution;
        }
        else
        {
            if (!_solutionContainerSystem.TryGetSolution(entityUid, condition.Solution, out _, out solution))
                return 0;
        }

        var quant = solution.GetTotalPrototypeQuantity(condition.Reagent);

        if (condition.Max == condition.Min)
            return quant == condition.Max ? 1 : 0;

        return ((quant - condition.Min) / (condition.Max - condition.Min)).Float();
    }
}
