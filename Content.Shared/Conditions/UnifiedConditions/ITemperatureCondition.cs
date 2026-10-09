using Content.Shared.Chemistry.Components;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;

namespace Content.Shared.Conditions.UnifiedConditions;

/// <summary>
/// A condition checked against <see cref="TemperatureComponent" /> and <see cref="SolutionComponent" /> for value.
/// </summary>
public interface ITemperatureCondition : ICondition, IConditionWithDefaultSatisfactionRule
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
public sealed partial class TemperatureEntityConditionSystem : ConditionEvaluatorSystem<ITemperatureCondition>
{
    public override float Evaluate(ITemperatureCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        TryComp(entityUid, out TemperatureComponent? temperatureComponent);
        TryComp(entityUid, out SolutionComponent? solutionComponent);
        float temperature;
        if (temperatureComponent != null && solutionComponent != null)
        {
            //get the average for good measure. really there only should ever be either.
            temperature = (temperatureComponent.Temperature + solutionComponent.Solution.Temperature) / 2;
        }
        else
        {
            temperature = (temperatureComponent?.Temperature ?? 0) + (solutionComponent?.Solution.Temperature ?? 0);
        }

        if (condition.Min - condition.Max < 0.0001)
            return temperature - condition.Max < 0.001 ? 1 : 0;

        return (temperature - condition.Min) /
               (condition.Max - condition.Min);
    }
}
