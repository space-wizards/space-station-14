using Content.Shared.Chemistry.Components;
using Content.Shared.Conditions;
using Content.Shared.Conditions.Interfaces;
using Content.Shared.Temperature.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface ITemperatureCondition : ICondition<ITemperatureCondition>, IWithBoundary
{
    /// <summary>
    /// Minimum allowed temperature
    /// </summary>
    float Min { get; }

    /// <summary>
    /// Maximum allowed temperature
    /// </summary>
    float Max { get; }

    float IWithBoundary.LowerBound => 0;

    bool IWithBoundary.IncludeLowerBound => true;

    float IWithBoundary.UpperBound => 1;

    bool IWithBoundary.IncludeUpperBound => true;
}

/// <summary>
/// Evaluates <see cref="ITemperatureCondition"/>
/// </summary>
public sealed partial class TemperatureEntityConditionSystem : EntitySystem
{
    /// <summary>
    /// Returns the proportional value of components temperature to the conditions bound.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void Condition(Entity<TemperatureComponent> entity, ref ConditionEvaluationEvent<ITemperatureCondition> args)
    {
        args.Value = (entity.Comp.Temperature - args.Condition.Min) / (args.Condition.Max - args.Condition.Min);

        args.Handled = true;
    }

    /// <summary>
    /// Returns the proportional value of components solutions temperature to the conditions bound.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void Condition(Entity<SolutionComponent> entity, ref ConditionEvaluationEvent<ITemperatureCondition> args)
    {
        args.Value = (entity.Comp.Solution.Temperature - args.Condition.Min) / (args.Condition.Max - args.Condition.Min);

        args.Handled = true;
    }
}
