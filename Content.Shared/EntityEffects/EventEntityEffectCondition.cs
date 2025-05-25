namespace Content.Shared.EntityEffects;

public abstract partial class EventEntityEffectCondition<T> : EntityEffectCondition where T : EventEntityEffectCondition<T>
{
    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (this is not T type)
            return false;

        var evt = new CheckEntityEffectConditionEvent<T> { Condition = type, Args = args };
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
        return evt.Result;
    }
}
