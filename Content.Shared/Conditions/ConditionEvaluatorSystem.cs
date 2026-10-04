namespace Content.Shared.Conditions;


public abstract class ConditionEvaluatorSystem:EntitySystem
{
    public abstract float EvaluateAny(ICondition condition, EntityUid entityUid, EntityUid? sourceEntity = null);
}

/// <summary>
/// The definition to mark an entity system as a condition evaluator.
/// </summary>
/// <typeparam name="TCondition"></typeparam>
public abstract class ConditionEvaluatorSystem<TCondition> : ConditionEvaluatorSystem where TCondition : ICondition
{

    public override float EvaluateAny(ICondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (condition is not TCondition actualCondition)
            return 0;
        return Evaluate(actualCondition, entityUid, sourceEntity);
    }

    /// <summary>
    /// The main evaluation function called by <see cref="SharedConditionEvaluationSystem.EvaluateCondition" />
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="entityUid"></param>
    /// <param name="sourceEntity"></param>
    /// <returns></returns>
    public abstract float Evaluate(TCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null);
}
