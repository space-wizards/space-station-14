namespace Content.Shared.Chat;

public abstract class ListenerEntitySystem<T> : EntitySystem where T : ListenerComponent
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ListenerConsumeEvent>(OnListenerConsumeEvent);
        SubscribeLocalEvent<GetListenerConsumersEvent>(OnGetListenerConsumerEvent);
    }

    /// <summary>
    /// Used to gather all entities that have components with ListenerEntitySystems.
    /// </summary>
    protected void OnGetListenerConsumerEvent(ref GetListenerConsumersEvent ev)
    {
        var query = AllEntityQuery<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            ev.Entities.Add(uid);
        }
    }

    /// <summary>
    /// Runs when a message has been "heard" by the entity, and filters out any that should not be received by this system specifically.
    /// If <see cref="ListenerComponent.FilteredTypes"/> is null, all messages heard by the entity are accepted.
    /// </summary>
    protected void OnListenerConsumeEvent(EntityUid uid, T component, ListenerConsumeEvent args)
    {
        if (component.FilteredTypes == null || (component.FilteredTypes & args.ChatMedium) != 0)
            OnListenerMessageReceived(uid, component, args);
    }

    /// <summary>
    /// Runs the desired behavior for when a message has been received and accepted by the listening component.
    /// </summary>
    public abstract void OnListenerMessageReceived(EntityUid uid, T component, ListenerConsumeEvent args);
}
