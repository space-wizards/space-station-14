using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Conditions.HelperConditions;

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
        var evt = condition.WrapInEvent(entityUid, sourceEntity);
        if (evt == null)
            return 0f;
        // Use event to evaluate condition on entity.
        RaiseLocalEvent(entityUid, (object)evt);
        // Verity that the event was actually handled
        if (!evt.Handled)
        {
            //Add logging here?
        }

        //return response.
        return evt.Value;
    }

    /// <summary>
    /// Evaluates a condition against an entity given an optional source entity.
    /// Using additional interfaces on the condition to turn the result of the evaluation into a bool.
    /// </summary>
    /// <param name="condition">The condition to check</param>
    /// <param name="entityUid">The entity to check against</param>
    /// <param name="sourceEntity">An optional entity, likely the raiser of the evaluation</param>
    /// <returns></returns>
    /// <seealso cref="IWithInverted"/>
    /// <seealso cref="IWithBoundary"/>
    /// <seealso cref="IWithThreshold"/>
    public bool IsConditionSatisfied(ICondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return IsConditionSatisfied(condition, entityUid, out _, sourceEntity);
    }

    /// <summary>
    /// Evaluates a condition against an entity given an optional source entity.
    /// Using additional interfaces on the condition to turn the result of the evaluation into a bool.
    /// </summary>
    /// <param name="condition">The condition to check</param>
    /// <param name="entityUid">The entity to check against</param>
    /// <param name="scale"></param>
    /// <param name="sourceEntity">An optional entity, likely the raiser of the evaluation</param>
    /// <returns></returns>
    /// <remarks>Use this version if you want to use both the scale and satisfy result. Like EntityEffect check and then Scaling.</remarks>
    /// <seealso cref="IWithInverted"/>
    /// <seealso cref="IWithBoundary"/>
    /// <seealso cref="IWithThreshold"/>
    public bool IsConditionSatisfied(ICondition condition,
        EntityUid entityUid,
        out float scale,
        EntityUid? sourceEntity = null)
    {
        scale = EvaluateCondition(condition, entityUid, sourceEntity);
        return EvalConditionWithInverted(condition, IsConditionSatisfied(condition, scale));
    }

    /// <summary>
    /// helper function to
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private bool IsConditionSatisfied(ICondition condition, float value)
    {
        //check scale now against the conditions limits.
        var satisfier = condition.Satisfier;
        if (satisfier == null &&
            condition is IConditionWithDefaultSatisfactionRule conditionWithDefaultSatisfactionRule)
            satisfier = conditionWithDefaultSatisfactionRule.GetDefaultSatisfier();
        if (satisfier == null)
            return value != 0;
        return satisfier.Inverted != satisfier.IsSatisfied(value);
    }

    /// <summary>
    /// Helper for Inverted Tag
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private bool EvalConditionWithInverted(ICondition condition, bool value)
    {
        if (condition is IWithInverted invertedCondition)
            return invertedCondition.Inverted != value;
        return value;
    }
}
