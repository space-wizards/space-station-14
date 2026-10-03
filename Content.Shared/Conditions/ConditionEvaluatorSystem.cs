namespace Content.Shared.Conditions;

/// <summary>
/// The definition to mark an entity system as a condition evaluator.
/// </summary>
/// <typeparam name="TCondition"></typeparam>
public abstract class ConditionEvaluatorSystem<TCondition> : EntitySystem where TCondition : ICondition
{
    /// <summary>
    /// The main evaluation function called by <see cref="SharedConditionEvaluationSystem.EvaluateCondition"/>
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="entityUid"></param>
    /// <param name="sourceEntity"></param>
    /// <returns></returns>
    public abstract float Evaluate(TCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null);
}

