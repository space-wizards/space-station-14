using Content.Shared.Chemistry.Components;
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

    /// <inheritdoc/>
    Satisfier.Satisfier IConditionWithDefaultSatisfactionRule.GetDefaultSatisfier()
    {
        return new WithBoundary()
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
    [SubscribeLocalEvent]
    private void Condition(Entity<SolutionComponent> entity, ref ConditionEvaluationEvent<IReagentCondition> args)
    {
        var soln = entity.Comp.Solution;

        var quant = soln.GetTotalPrototypeQuantity(args.Condition.Reagent);

        args.Value = ((quant - args.Condition.Min) / (args.Condition.Max - args.Condition.Min)).Float();

        args.Handled = true;
    }
}
