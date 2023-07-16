

namespace Content.Shared.StatusEffects;

/// <summary>
/// This is used in relays to carry over the original event's entity.
/// </summary>
public sealed class StatusEffectRelayEvent<TEvent> : EntityEventArgs
{
    public readonly TEvent Args;
    public EntityUid Afflicted;

    public StatusEffectRelayEvent(TEvent args, EntityUid afflicted)
    {
        Args = args;
        Afflicted = afflicted;
    }
}

/// <summary>
/// For when an effect runs out.
/// </summary>
public sealed class StatusEffectTimeoutEvent : EntityEventArgs
{
    public EntityUid Afflicted;

    public StatusEffectTimeoutEvent(EntityUid afflicted)
    {
        Afflicted = afflicted;
    }
}

/// <summary>
/// Used for active effects.
/// </summary>
public sealed class StatusEffectActivateEvent : EntityEventArgs
{
    public EntityUid Afflicted;
    public StatusEffectActivateEvent(EntityUid victim)
    {
        Afflicted = afflicted;
    }
}
