using Content.Shared.Chemistry.Components;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.Temperature.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

/// <summary>
/// A condition checked against <see cref="TemperatureComponent" /> and <see cref="SolutionComponent" /> for value.
/// </summary>
public interface ITemperatureCondition : ICondition<ITemperatureCondition>, IConditionWithDefaultSatisfactionRule
{
    /// <summary>
    /// Minimum allowed temperature
    /// </summary>
    float Min { get; }

    /// <summary>
    /// Maximum allowed temperature
    /// </summary>
    float Max { get; }

    /// <inheritdoc />
    Satisfier.Satisfier IConditionWithDefaultSatisfactionRule.GetDefaultSatisfier()
    {
        return new WithBoundary
        {
            LowerBound = 0,
            UpperBound = 1,
            IncludeLowerBound = true,
            IncludeUpperBound = true,
        };
    }
}

/// <summary>
/// Evaluates <see cref="ITemperatureCondition" />
/// </summary>
public sealed partial class TemperatureEntityConditionSystem : EntitySystem
{
    /// <summary>
    /// Returns the proportional value of components temperature to the conditions bound.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void Condition(Entity<TemperatureComponent> entity,
        ref ConditionEvaluationEvent<ITemperatureCondition> args)
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
        args.Value = (entity.Comp.Solution.Temperature - args.Condition.Min) /
                     (args.Condition.Max - args.Condition.Min);

        args.Handled = true;
    }
}
