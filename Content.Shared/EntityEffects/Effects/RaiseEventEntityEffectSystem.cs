namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Raises an event on the entity.
/// </summary>
public sealed partial class RaiseEventEntityEffectSystem : EntityEffectSystem<TransformComponent, RaiseEvent>
{
    [Dependency] private EntityManager _entMan = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<RaiseEvent> args)
    {
        var directed = args.Effect.Directed;
        var broadcast = args.Effect.Broadcast;

        if (!directed && !broadcast)
            return;

        if(directed)
            _entMan.EventBus.RaiseLocalEvent(entity, (object)args.Effect.Event);

        if(broadcast)
            _entMan.EventBus.RaiseEvent(EventSource.Local, (object)args.Effect.Event);
    }
}

public sealed partial class RaiseEvent : EntityEffectBase<RaiseEvent>
{
    [DataField(required: true)]
    public required EntityEventArgs Event;

    [DataField]
    public bool Directed = true;

    [DataField]
    public bool Broadcast = true;
}
