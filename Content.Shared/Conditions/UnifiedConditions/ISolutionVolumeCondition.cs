using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.FixedPoint;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface ISolutionVolumeCondition : ICondition,
    IConditionWithDefaultSatisfactionRule
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
public sealed partial class SolutionVolumeConditionSystem : ConditionEvaluatorSystem<ISolutionVolumeCondition>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override float Evaluate(ISolutionVolumeCondition condition,
        EntityUid entityUid,
        EntityUid? sourceEntity = null)
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

        if (condition.Max == condition.Min)
            return solution.Volume == condition.Max ? 1 : 0;

        return ((solution.Volume - condition.Min) / (condition.Max - condition.Min)).Float();
    }
}
