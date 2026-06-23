using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class SharedEntityConditionsSystem : EntitySystem
{
    private Dictionary<Type, EntityConditionHandler> _handlers = new();

    public void RegisterHandler(EntityConditionHandler handler)
    {
        _handlers[handler.ConditionType] = handler;
    }

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
            if (!TryCondition(target, condition, sourceEnt))
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
        return condition.Inverted != CheckCondition(target, condition, sourceEnt);
    }

    private bool CheckCondition<T>(EntityUid target, T condition, EntityUid? sourceEnt = null) where T : EntityCondition
    {
        if (_handlers.TryGetValue(condition.GetType(), out var handler))
            return handler.CheckCondition(target, condition, sourceEnt);
        return false;
    }
}

/// <summary>
/// Abstract base class for entity condition handlers.
/// Extends EntitySystem so concrete handlers are proper engine systems.
/// </summary>
public abstract partial class EntityConditionHandler : EntitySystem
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    public abstract Type ConditionType { get; }

    public abstract bool CheckCondition(EntityUid target, EntityCondition condition, EntityUid? sourceEnt = null);

    /// <inheritdoc/>
    public override void Initialize()
    {
        _conditions.RegisterHandler(this);
    }
}

/// <summary>
/// This is a basic abstract entity effect containing all the data an entity effect needs to affect entities with effects...
/// </summary>
/// <typeparam name="T">The Component that is required for the effect</typeparam>
/// <typeparam name="TCon">The Condition we're testing</typeparam>
public abstract partial class EntityConditionSystem<T, TCon> : EntityConditionHandler
    where T : Component where TCon : EntityCondition
{
    [Dependency] private EntityQuery<T> _query = default!;

    public override Type ConditionType => typeof(TCon);

    protected abstract void Condition(Entity<T> entity, TCon condition, EntityUid? sourceEnt, ref bool result);

    public override bool CheckCondition(EntityUid target, EntityCondition condition, EntityUid? sourceEnt = null)
    {
        if (condition is not TCon typed)
            return false;
        if (!_query.TryGetComponent(target, out var comp))
            return false;
        var result = false;
        Condition((target, comp), typed, sourceEnt, ref result);
        return result;
    }
}

/// <summary>
/// A basic condition which can be checked for on an entity via events.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class EntityCondition
{
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
