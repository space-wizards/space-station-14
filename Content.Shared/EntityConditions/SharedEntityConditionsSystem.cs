namespace Content.Shared.EntityEffects;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class SharedEntityConditionsSystem : EntitySystem, IEntityConditionRaiser
{
    public override void Initialize()
    {

    }

    public bool TryConditions(EntityUid target, AnyEntityCondition[]? conditions)
    {
        if (conditions == null)
            return true;

        foreach (var condition in conditions)
        {
            if (!condition.RaiseEvent(target, this))
                return false;
        }

        return true;
    }

    public bool RaiseConditionEvent<T>(EntityUid target, T effect) where T : EntityConditionBase<T>
    {
        var effectEv = new EntityConditionEvent<T>(effect);
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

public interface IEntityConditionRaiser
{
    bool RaiseConditionEvent<T>(EntityUid target, T effect) where T : EntityConditionBase<T>;
}

public abstract partial class EntityConditionBase<T> : AnyEntityCondition where T : EntityConditionBase<T>
{
    public override bool RaiseEvent(EntityUid target, IEntityConditionRaiser raiser)
    {
        if (this is not T type)
            return false;

        // If the result of the event matches the result we're looking for then we pass.
        return type.Condition = raiser.RaiseConditionEvent(target, type);
    }
}

// This exists so we can store entity effects in list and raise events without type erasure.
public abstract partial class AnyEntityCondition
{
    public abstract bool RaiseEvent(EntityUid target, IEntityConditionRaiser raiser);

    // TODO: Rename this shit it's ass.
    [DataField]
    public bool Condition = true;

    [DataField]
    public readonly string EntityConditionGuidebookText = String.Empty;
}

/// <summary>
/// An Event carrying an entity effect.
/// </summary>
/// <param name="Condition">The Condition we're checking</param>
[ByRefEvent]
public record struct EntityConditionEvent<T>(T Condition) where T : EntityConditionBase<T>
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
}

// TODO: Make a struct for or/and/xor linked conditions.
