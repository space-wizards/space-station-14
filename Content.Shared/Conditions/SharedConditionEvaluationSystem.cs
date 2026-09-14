using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Conditions;

/// <summary>
/// The central API through which <see cref="ICondition"/>s can be evaluated.
/// </summary>
public sealed partial class SharedConditionEvaluationSystem : EntitySystem
{

    /// <summary>
    /// Evaluates a condition against an entity given an optional source entity.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="entityUid">The entity on which the condition is to be tested.</param>
    /// <param name="sourceEntity"></param>
    /// <returns>A factor representing how much the condition is satisfied. For most binary conditions, this is 0 and 1, but anything gray it could be any arbitrary value</returns>
    /// <exception cref="NotImplementedException">A condition that cannot be evaluated should not exist. Either you using it on an entity missing necessary components or there is no system to evaluate the condition</exception>
    public float EvaluateCondition(ICondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        //make the event using our cached building function.
        var evt = new ConditionEvaluationEvent(condition, entityUid, sourceEntity);
        // Use event to evaluate condition on entity.
        RaiseLocalEvent(entityUid, ref evt);
        // Verity that the event was actually handled
        if (!evt.Handled)
        {
            throw new NotImplementedException($"Condition of type {condition.ConditionType.Name} has no evaluator.");
        }
        //return response.
        return evt.Value;
    }

    public bool IsConditionSatisfied(ICondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        var scale= EvaluateCondition(condition, entityUid, sourceEntity);
        //check scale now against the conditions limits.
        switch (condition)
        {
            case IConditionWithThreshold threshold:
                return EvalConditionWithThreshold(threshold,scale);
            case IConditionWithBoundary boundary:
                return EvalConditionWithBoundary(boundary,scale);
            //fallback literally just 0==false. most binary conditions won't have any added hints and use this.
            default:
                return scale != 0;
        }
    }

    private bool EvalConditionWithBoundary(IConditionWithBoundary boundary, float scale)
    {
        if ((boundary.IncludeLowerBound && scale < boundary.LowerBound) || scale <= boundary.LowerBound)
            return boundary.Inverted;
        if ((boundary.IncludeUpperBound &&  boundary.UpperBound<scale) || boundary.LowerBound<=scale)
            return boundary.Inverted;
        return !boundary.Inverted;
    }

    private bool EvalConditionWithThreshold(IConditionWithThreshold condition, float value)
    {
        switch (condition.Comparison)
        {
            case IConditionWithThreshold.Comparator.Less:
                return  value < condition.Threshold;
            case IConditionWithThreshold.Comparator.LessEqual:
                return  value <= condition.Threshold;
            case IConditionWithThreshold.Comparator.Equal:
                return  value.Equals(condition.Threshold);
            case IConditionWithThreshold.Comparator.Greater:
                return  value > condition.Threshold;
            case IConditionWithThreshold.Comparator.GreaterEqual:
                return  value >= condition.Threshold;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}
