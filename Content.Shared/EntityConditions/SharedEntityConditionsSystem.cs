using Content.Shared.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class SharedEntityConditionsSystem : EntitySystem
{

    [Dependency] private SharedConditionEvaluationSystem _conditionSystem=default!;

    /// <summary>
    /// Checks a list of conditions to verify that they all return true.
    /// </summary>
    /// <param name="target">Target entity we're checking conditions on</param>
    /// <param name="conditions">Conditions we're checking</param>
    /// <param name="sourceEnt">An optional "source entity" which is checking the condition on the entity this is being raised to.
    /// Sometimes needed for additional context with conditions.</param>
    /// <returns>Returns true if all conditions return true, false if any fail</returns>
    public bool TryConditions<T>(EntityUid target, T[]? conditions, EntityUid? sourceEnt = null) where T : EntityCondition
    {
        // If there's no conditions we can't fail any of them...
        if (conditions == null)
            return true;

        foreach (var condition in conditions)
        {
            if (!_conditionSystem.IsConditionSatisfied(condition, target, sourceEnt))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks a list of conditions to see if any are true.
    /// </summary>
    /// <param name="target">Target entity we're checking conditions on</param>
    /// <param name="conditions">Conditions we're checking</param>
    /// <param name="sourceEnt">An optional "source entity" which is checking the condition on the entity this is being raised to.
    /// Sometimes needed for additional context with conditions.</param>
    /// <returns>Returns true if any conditions return true</returns>
    public bool TryAnyCondition<T>(EntityUid target, T[]? conditions, EntityUid? sourceEnt = null) where T : EntityCondition
    {
        // If there's no conditions we can't meet any of them...
        if (conditions == null)
            return false;

        foreach (var condition in conditions)
        {
            if (TryCondition(target, condition, sourceEnt))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks a single <see cref="EntityCondition"/> on an entity.
    /// </summary>
    /// <param name="target">Target entity we're checking conditions on</param>
    /// <param name="condition">Condition we're checking</param>
    /// <param name="sourceEnt">An optional "source entity" which is checking the condition on the entity this is being raised to.
    /// Sometimes needed for additional context with conditions.</param>
    /// <returns>Returns true if we meet the condition and false otherwise</returns>
    public bool TryCondition<T>(EntityUid target, T condition, EntityUid? sourceEnt = null) where T : EntityCondition
    {
        return _conditionSystem.IsConditionSatisfied(condition,target, sourceEnt);
    }

}

/// <summary>
/// A basic condition which can be checked for on an entity via events.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class EntityCondition : ICondition, IWithInverted
{

    /// <summary>
    /// If true, invert the result. So false returns true and true returns false!
    /// </summary>
    [DataField]
    public bool Inverted { get; set; }

    /// <summary>
    /// A basic description of this condition, which displays in the guidebook.
    /// </summary>
    public abstract string EntityConditionGuidebookText(IPrototypeManager prototype);


    public abstract Type ConditionType { get; }
}

public abstract partial class EntityConditionBase<TCondition> : EntityCondition, ICondition<TCondition>
{
    public override Type ConditionType => typeof(TCondition);
}
