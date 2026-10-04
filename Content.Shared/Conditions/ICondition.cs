namespace Content.Shared.Conditions;

/// <summary>
/// Interface to mark a data structure as a condition for shared storage and evaluation.
/// If deriving an event directly and solely from this, remember to have an appropriate <see cref="ConditionEvaluatorSystem{TCondition}"/> implemeneted.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// If set, rather than just checkin on value != 0, we use some more intricate logic.
    /// </summary>
    Satisfier.Satisfier? Satisfier { get; }
}

/// <summary>
/// A condition which for some reason must be evaluated by an event rather than <see cref="ConditionEvaluatorSystem{TCondition}"/>
/// It is advised to use <see cref="IConditionByEvent{TCondition}" />.
/// </summary>
public interface IConditionByEvent : ICondition
{
    /// <summary>
    /// Used to help the evaluation system to raise an event to evaluate this condition.
    /// </summary>
    ConditionEvaluationEvent? WrapInEvent(EntityUid entity, EntityUid? sourceEntity);
}

/// <summary>
/// The "strongly" typed version of <see cref="IConditionByEvent"/>.
/// Assign directly to condition classes or interfaces for the case of shared legacy conditions.
/// </summary>
/// <typeparam name="TCondition">
/// The strong type of the condition, which at time of inception, all evaluating systems will
/// look for.
/// </typeparam>
public interface IConditionByEvent<TCondition> : IConditionByEvent where TCondition : IConditionByEvent
{
}

/// <summary>
/// Tag on conditions with expected Satisfier behavior.
/// </summary>
public interface IConditionWithDefaultSatisfactionRule : ICondition
{
    /// <summary>
    /// Gives a default satisfier, which the evaluation system will use if <see cref="IConditionByEvent.Satisfier" /> is not
    /// set.
    /// </summary>
    /// <returns></returns>
    /// <remarks>While you can set <see cref="ICondition.Satisfier"/> as part of the conditions constructor, if you expect the satisfier to be mutable, use this.</remarks>
    Satisfier.Satisfier GetDefaultSatisfier();
}
