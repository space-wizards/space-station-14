using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Player;

namespace Content.Shared.StatusEffectNew;

public abstract partial class SharedStatusEffectsSystem
{
    protected void InitializeRelay()
    {
        SubscribeLocalEvent<StatusEffectContainerComponent, LocalPlayerAttachedEvent>(RelayStatusEffectEvent);
        SubscribeLocalEvent<StatusEffectContainerComponent, LocalPlayerDetachedEvent>(RelayStatusEffectEvent);
    }

    protected void RefRelayStatusEffectEvent<T>(EntityUid uid, StatusEffectContainerComponent component, ref T args) where T : EntityEventArgs
    {
        RelayEvent((uid, component), ref args);
    }

    protected void RelayStatusEffectEvent<T>(EntityUid uid, StatusEffectContainerComponent component, T args) where T : EntityEventArgs
    {
        RelayEvent((uid, component), ref args);
    }

    public void RelayEvent<T>(Entity<StatusEffectContainerComponent> statusEffect, ref T args) where T : EntityEventArgs
    {
        // this copies the by-ref event if it is a struct
        var ev = new StatusEffectRelayedEvent<T>(args);
        foreach (var activeEffect in statusEffect.Comp.ActiveStatusEffects)
        {
            RaiseLocalEvent(activeEffect, ev);
        }
        // and now we copy it back
        args = ev.Args;
    }

    public void RelayEvent<T>(Entity<StatusEffectContainerComponent> statusEffect, T args) where T : EntityEventArgs
    {
        // this copies the by-ref event if it is a struct
        var ev = new StatusEffectRelayedEvent<T>(args);
        foreach (var activeEffect in statusEffect.Comp.ActiveStatusEffects)
        {
            RaiseLocalEvent(activeEffect, ev);
        }
    }
}

/// <summary>
///     Event wrapper for relayed events.
/// </summary>
public sealed class StatusEffectRelayedEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;

    public StatusEffectRelayedEvent(TEvent args)
    {
        Args = args;
    }
}
