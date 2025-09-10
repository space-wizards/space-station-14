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

    public void RaiseConditionEvent<T>(EntityUid target, T effect) where T : EntityConditionBase<T>
    {
        var effectEv = new EntityConditionEvent<T>(effect);
        RaiseLocalEvent(target, ref effectEv);
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
    void RaiseConditionEvent<T>(EntityUid target, T effect) where T : EntityConditionBase<T>;
}

public abstract partial class EntityConditionBase<T> : AnyEntityCondition where T : EntityConditionBase<T>
{
    public override void RaiseEvent(EntityUid target, IEntityConditionRaiser raiser)
    {
        if (this is not T type)
            return;

        raiser.RaiseConditionEvent(target, type);
    }
}

// This exists so we can store entity effects in list and raise events without type erasure.
public abstract partial class AnyEntityCondition
{
    public abstract void RaiseEvent(EntityUid target, IEntityConditionRaiser raiser);

    [DataField]
    public float Probability = 1.0f;

    [DataField]
    public readonly string EntityConditionGuidebookText = String.Empty;

}

/// <summary>
/// An Event carrying an entity effect.
/// </summary>
/// <param name="Effect">The Effect</param>
/// <param name="Scale">A strength scalar for the effect, defaults to 1 and typically only goes under for incomplete reactions.</param>
[ByRefEvent]
public readonly record struct EntityConditionEvent<T>(T Effect, float Scale = 1f) where T : EntityConditionBase<T>;
