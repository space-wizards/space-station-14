namespace Content.Shared.Conditions;

/// <summary>
/// Interface to mark a data structure as a condition for shared storage and evaluation.
/// It is advised to use <see cref="ICondition{TCondition}"/>.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// Used to help the evaluation system to raise an event to evaluate this condition.
    /// </summary>
    ConditionEvaluationEvent? WrapInEvent(EntityUid entity, EntityUid? sourceEntity);
}

/// <summary>
/// The "strongly" typed version of ICondition.
/// Assign directly to condition classes or interfaces for the case of shared legacy conditions.
/// </summary>
/// <typeparam name="TCondition">The strong type of the condition, which at time of inception, all evaluating systems will look for.</typeparam>
public interface ICondition<TCondition> : ICondition where TCondition : ICondition
{

}

public interface IConditionWithSatisfactionRule : ICondition
{
    bool IsValueSatisfactory(float value);
}
