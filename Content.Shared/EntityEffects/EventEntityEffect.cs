namespace Content.Shared.EntityEffects;

public abstract partial class EventEntityEffect<T> : EntityEffect where T : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (this is not T type)
            return;
        var ev = new ExecuteEntityEffectEvent<T>(type, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
    }
}
