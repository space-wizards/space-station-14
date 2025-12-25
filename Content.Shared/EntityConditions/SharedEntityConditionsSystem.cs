using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

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
    /// <param name="source">Entity that initiated the check, it is passed to <paramref name="conditions"/> of optional
    /// use. </param>
    /// <returns>Returns true if all conditions return true, false if any fail</returns>
    public bool TryConditions(EntityUid target, EntityCondition[]? conditions, EntityUid? source = null)
    {
        // If there's no conditions we can't fail any of them...
        if (conditions == null)
            return true;

        foreach (var condition in conditions)
        {
            if (!TryCondition(target, condition, out var result, source))
                continue;

            if (!result.Value)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks a list of conditions to see if any are true.
    /// </summary>
    /// <param name="target">Target entity we're checking conditions on</param>
    /// <param name="conditions">Conditions we're checking</param>
    /// <param name="source">Entity that initiated the check, it is passed to <paramref name="conditions"/> of optional
    /// use. </param>
    /// <returns>Returns true if any conditions return true</returns>
    public bool TryAnyCondition(EntityUid target, EntityCondition[]? conditions, EntityUid? source = null)
    {
        // If there's no conditions we can't meet any of them...
        if (conditions == null)
            return false;

        foreach (var condition in conditions)
        {
            if (!TryCondition(target, condition, out var result, source))
                continue;

            if (result.Value)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks a single <see cref="EntityCondition"/> on an entity.
    /// </summary>
    /// <param name="target">Target entity we're checking conditions on</param>
    /// <param name="condition">Condition we're checking</param>
    /// <param name="source">Entity that initiated the check, it is passed to <paramref name="conditions"/> of optional
    /// use. </param>
    /// <returns>Returns true if we meet the condition and false otherwise</returns>
    public bool TryCondition(EntityUid target, EntityCondition condition, EntityUid? source = null)
    {
        // If condition can't be applied, condition was not failed
        if (!TryCondition(target, condition, out var result, source))
            return true;

        return result.Value;
    }

    /// <summary>
    /// Checks a single <see cref="EntityCondition"/> on an entity, while providing information if check
    /// could even be properly performed.
    /// </summary>
    /// <param name="target">Target entity we're checking conditions on</param>
    /// <param name="condition">Condition we're checking</param>
    /// <param name="result">The result of condition check</param>
    /// <param name="source">Entity that initiated the check, it is passed to <paramref name="conditions"/> of optional
    /// use. </param>
    /// arguments</param>
    /// <returns>Returns true if we meet the condition was valid for provided <paramref name="parameters"/></returns>
    public bool TryCondition(EntityUid target, EntityCondition condition, [NotNullWhen(true)] out bool? result, EntityUid? source = null)
    {
        result = false;

        if (!condition.TryRaiseEvent(target, this, out var conditionResult, source))
            return false;

        result = condition.Inverted != conditionResult;
        return true;
    }

    /// <summary>
    /// Raises a condition to an entity. You should not be calling this unless you know what you're doing.
    /// </summary>
    public bool RaiseConditionEvent<T>(EntityUid target, T effect, EntityUid? source = null) where T : EntityConditionBase<T>
    {
        var effectEv = new EntityConditionEvent<T>(effect, source);
        RaiseLocalEvent(target, ref effectEv);
        return effectEv.Result;
    }
}

/// <summary>
/// This is a basic abstract entity effect containing all the data an entity effect needs to affect entities with effects...
/// </summary>
/// <typeparam name="T">The Component that is required for the effect</typeparam>
/// <typeparam name="TCon">The Condition we're testing</typeparam>
public abstract partial class EntityConditionSystem<T, TCon> : EntitySystem where T : Component where TCon : EntityConditionBase<TCon>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<T, EntityConditionEvent<TCon>>(Condition);
    }
    protected abstract void Condition(Entity<T> entity, ref EntityConditionEvent<TCon> args);
}

/// <summary>
/// Used to raise an EntityCondition without losing the type of condition.
/// </summary>
public interface IEntityConditionRaiser
{
    bool RaiseConditionEvent<T>(EntityUid target, T effect, EntityUid? source = null) where T : EntityConditionBase<T>;
}

/// <summary>
/// Used to store an <see cref="EntityCondition"/> so it can be raised without losing the type of the condition.
/// </summary>
/// <typeparam name="T">The Condition wer are raising.</typeparam>
public abstract partial class EntityConditionBase<T> : EntityCondition where T : EntityConditionBase<T>
{
    public override bool TryRaiseEvent(EntityUid target, IEntityConditionRaiser raiser, [NotNullWhen(true)] out bool? result, EntityUid? source = null)
    {
        result = false;

        if (this is not T type)
            return false;

        if (RequiresParameterSource && source == null)
            return false;

        // If the result of the event matches the result we're looking for then we pass.
        result = raiser.RaiseConditionEvent(target, type, source);
        return true;
    }
}

/// <summary>
/// A basic condition which can be checked for on an entity via events.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class EntityCondition
{
    public abstract bool TryRaiseEvent(EntityUid target, IEntityConditionRaiser raiser, [NotNullWhen(true)] out bool? result, EntityUid? source = null);
    public virtual bool RequiresParameterSource => false;

    /// <summary>
    /// If true, invert the result. So false returns true and true returns false!
    /// </summary>
    [DataField]
    public bool Inverted;

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
public record struct EntityConditionEvent<T>(T Condition, EntityUid? Source = null) where T : EntityConditionBase<T>
{
    /// <summary>
    /// The result of our check, defaults to false if nothing handles it.
    /// </summary>
    [DataField]
    public bool Result;

    /// <summary>
    /// The Condition being raised in this event
    /// </summary>
    public readonly T Condition = Condition;

    /// <summary>
    /// Parameter passed to <paramref name="Condition"/> containing
    /// entity that is performing the check.
    /// </summary>
    public EntityUid? Source = Source;
}
