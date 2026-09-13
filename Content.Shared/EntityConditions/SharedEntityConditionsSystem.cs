using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class SharedEntityConditionsSystem : EntitySystem, IEntityConditionRaiser
{
    /// <summary>
    /// Checks a list of conditions to verify that they all return true.
    /// </summary>
    /// <param name="target">Target entity we're checking conditions on</param>
    /// <param name="conditions">Conditions we're checking</param>
    /// <param name="sourceEnt">An optional "source entity" which is checking the condition on the entity this is being raised to.
    /// Sometimes needed for additional context with conditions.</param>
    /// <returns>Returns true if all conditions return true, false if any fail</returns>
    public bool TryConditions<T>(EntityUid target, T[]? conditions, EntityUid? sourceEnt = null)
        where T : EntityCondition
    {
        // If there's no conditions we can't fail any of them...
        if (conditions == null)
            return true;

        foreach (var condition in conditions)
        {
            if (!TryCondition(target, condition, sourceEnt))
                return false;
        }

        return true;
    }

    public bool TryCondition<TCondition, TData>(TData target, TCondition condition)
        where TCondition : EntityCondition
    {
        if (condition is not IArbitraryEvaluationEnabled<TData, TCondition> evaluatorInfo)
            return false;
        return (IoCManager.ResolveType(evaluatorInfo.EvaluatorType) as IArbitaryConditionEvaluator<TData, TCondition>)
            ?.DoesSatisfy(target, condition) ?? false;
    }

    public float TryConditionScale<TCondition, TData>(TData target, TCondition condition)
        where TCondition : EntityCondition
    {
        if (condition is not IArbitraryEvaluationEnabled<TData, TCondition> evaluatorInfo)
            return condition.ValueIfScaleNull;
        return (IoCManager.ResolveType(evaluatorInfo.EvaluatorType) as IArbitaryConditionEvaluator<TData, TCondition>)
            ?.Scale(target, condition) ?? condition.ValueIfScaleNull;
    }

    /// <summary>
    /// Checks a list of conditions to see if any are true.
    /// </summary>
    /// <param name="target">Target entity we're checking conditions on</param>
    /// <param name="conditions">Conditions we're checking</param>
    /// <param name="sourceEnt">An optional "source entity" which is checking the condition on the entity this is being raised to.
    /// Sometimes needed for additional context with conditions.</param>
    /// <returns>Returns true if any conditions return true</returns>
    public bool TryAnyCondition<T>(EntityUid target, T[]? conditions, EntityUid? sourceEnt = null)
        where T : EntityCondition
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
        return condition.Inverted != condition.RaiseEvent(target, this, sourceEnt);
    }

    /// <summary>
    /// Raises a condition to an entity. You should not be calling this unless you know what you're doing.
    /// </summary>
    public bool RaiseConditionEvent<TCondition>(EntityUid target, TCondition condition, EntityUid? sourceObj)
        where TCondition : EntityConditionBase<TCondition>
    {
        var effectEv = new EntityConditionEvent<TCondition>(condition, sourceObj);
        RaiseLocalEvent(target, ref effectEv);
        return effectEv.Result;
    }

    /// <summary>
    /// Raises a condition to an entity. You should not be calling this unless you know what you're doing.
    /// </summary>
    public float? RaiseConditionScaleEvent<TCondition>(EntityUid target,
        TCondition condition,
        EntityUid? sourceEnt) where TCondition : EntityConditionBase<TCondition>
    {
        var effectEv = new EntityConditionScaleEvent<TCondition>(condition, sourceEnt);
        RaiseLocalEvent(target, ref effectEv);
        return effectEv.Result;
    }
}

/// <summary>
/// This is a basic abstract entity effect containing all the data an entity effect needs to affect entities with effects...
/// </summary>
/// <typeparam name="T">The Component that is required for the effect</typeparam>
/// <typeparam name="TCon">The Condition we're testing</typeparam>
public abstract partial class EntityConditionSystem<T, TCon> : EntitySystem
    where T : Component where TCon : EntityConditionBase<TCon>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<T, EntityConditionEvent<TCon>>(Condition);
    }

    protected abstract void Condition(Entity<T> entity, ref EntityConditionEvent<TCon> args);
}

public interface IArbitaryConditionEvaluator<in TData, in TCondition> where TCondition : EntityCondition
{
    abstract bool DoesSatisfy(TData target, TCondition condition);

    abstract float? Scale(TData target, TCondition condition);
}

/// <summary>
/// Used to raise an EntityCondition without losing the type of condition.
/// </summary>
public interface IEntityConditionRaiser
{
    /// <summary>
    /// Fire a <see cref="EntityConditionEvent{TCondition}"/> on target, using condition and source object as parameter.
    /// </summary>
    /// <param name="target">Where the event will be raised</param>
    /// <param name="condition">The condition to be evaluated</param>
    /// <param name="sourceObject">The optional source object</param>
    /// <typeparam name="TCondition">Type of the condition</typeparam>
    /// <typeparam name="TSource">Type of the source. Expect <see cref="EntityUid"/> or <see cref="Solution"/>> but could be any other DataDefinition, if necessary in the future.</typeparam>
    /// <returns></returns>
    bool RaiseConditionEvent<TCondition>(EntityUid target, TCondition condition, EntityUid? sourceObject)
        where TCondition : EntityConditionBase<TCondition>;

    float? RaiseConditionScaleEvent<TCondition>(EntityUid target, TCondition condition, EntityUid? sourceObject)
        where TCondition : EntityConditionBase<TCondition>;
}

/// <summary>
/// Used to store an <see cref="EntityCondition"/> so it can be raised without losing the type of the condition.
/// </summary>
/// <typeparam name="T">The Condition wer are raising.</typeparam>
public abstract partial class EntityConditionBase<T> : EntityCondition where T : EntityConditionBase<T>
{
    public override bool RaiseEvent(EntityUid target, IEntityConditionRaiser raiser, EntityUid? sourceObj)
    {
        if (this is not T type)
            return false;

        // If the result of the event matches the result we're looking for then we pass.
        return raiser.RaiseConditionEvent(target, type, sourceObj);
    }

    public override float RaiseScaleEvent(EntityUid target, IEntityConditionRaiser raiser, EntityUid? sourceObj)
    {
        if (this is not T type)
            return ValueIfScaleNull;

        // If the result of the event matches the result we're looking for then we pass.
        return raiser.RaiseConditionScaleEvent(target, type, sourceObj) ?? ValueIfScaleNull;
    }
}

/// <summary>
/// Use <see cref="IArbitraryEvaluationEnabled{TData,TCondition,TEvaluator}"/> instead!
/// </summary>
/// <typeparam name="TData"></typeparam>
/// <typeparam name="TCondition"></typeparam>
public interface IArbitraryEvaluationEnabled<TData, TCondition>
    where TCondition : EntityCondition
{
    /// <summary>
    /// Type of the Evaluating System for TCondition to be resolved from <see cref="IoCManager"/>
    /// </summary>
    Type EvaluatorType { get; }
}

/// <summary>
/// Attach to a condition with a fixed <see cref="EntitySystem"/> implementing <see cref="IArbitaryConditionEvaluator{TData,TCondition}"/> to enable evaluation of the condition with arbitrary data.
/// </summary>
/// <typeparam name="TData"></typeparam>
/// <typeparam name="TCondition"></typeparam>
/// <typeparam name="TEvaluator"></typeparam>
public interface IArbitraryEvaluationEnabled<TData, TCondition, TEvaluator>
    : IArbitraryEvaluationEnabled<TData, TCondition>
    where TCondition : EntityCondition
    where TEvaluator : EntitySystem, IArbitaryConditionEvaluator<TData, TCondition>
{
    /// <inheritdoc/>
    Type IArbitraryEvaluationEnabled<TData, TCondition>.EvaluatorType => typeof(TEvaluator);
}

/// <summary>
/// A basic condition which can be checked for on an entity via events.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class EntityCondition
{
    /// <summary>
    /// Check this condition on a target.
    /// </summary>
    public abstract bool RaiseEvent(EntityUid target, IEntityConditionRaiser raiser, EntityUid? sourceObj);

    /// <summary>
    /// Check this condition on a target.
    /// </summary>
    public abstract float RaiseScaleEvent(EntityUid target, IEntityConditionRaiser raiser, EntityUid? sourceObj);

    /// <summary>
    /// If true, invert the result. So false returns true and true returns false!
    /// </summary>
    [DataField]
    public bool Inverted;

    /// <summary>
    /// The Value if <see cref="RaiseScaleEvent"/> returns null.
    /// Thus depending on the context/use of the condition, failure to evaluate this condition can be treated as 0, 1, etc.
    /// </summary>
    [DataField]
    public float ValueIfScaleNull;

    /// <summary>
    /// A basic description of this condition, which displays in the guidebook.
    /// </summary>
    public abstract string EntityConditionGuidebookText(IPrototypeManager prototype);
}

/// <summary>
/// An Event carrying an entity effect.
/// </summary>
/// <param name="Condition">The Condition we're checking</param>
[ByRefEvent]
[DataRecord]
public partial record struct EntityConditionEvent<TCondition>(TCondition Condition, EntityUid? SourceEnt)
    where TCondition : EntityConditionBase<TCondition>
{
    /// <summary>
    /// The result of our check, defaults to false if nothing handles it.
    /// </summary>
    [DataField]
    public bool Result;

    /// <summary>
    /// The Condition being raised in this event
    /// </summary>
    public readonly TCondition Condition = Condition;

    /// <summary>
    /// An optional "source object" which is checking the condition on the entity this is being raised to.
    /// Sometimes needed for additional context with conditions.
    /// This can be an EntityUID, a solution or any other item.
    /// </summary>
    public readonly EntityUid? SourceEnt = SourceEnt;
}

/// <summary>
/// An Event carrying an entity effect.
/// </summary>
/// <param name="Condition">The Condition we're checking</param>
[ByRefEvent]
[DataRecord]
public partial record struct EntityConditionScaleEvent<TCondition>(TCondition Condition, EntityUid? SourceEnt)
    where TCondition : EntityConditionBase<TCondition>
{
    /// <summary>
    /// The result of the check, null if was not evaluated.
    /// This is treated as either a 0 in additive context and 1 or 0 in a multiplicative context.
    /// </summary>
    [DataField]
    public float? Result = null;

    /// <summary>
    /// The Condition being raised in this event
    /// </summary>
    public readonly TCondition Condition = Condition;

    /// <summary>
    /// An optional "source object" which is checking the condition on the entity this is being raised to.
    /// Sometimes needed for additional context with conditions.
    /// This can be an EntityUID, a solution or any other item.
    /// </summary>
    public readonly EntityUid? SourceEnt = SourceEnt;
}
