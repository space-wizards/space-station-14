using Content.Shared.Actions.Components;
using Content.Shared.Ghost;
using Content.Shared.Mobs;

namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<ActionsComponent, MobStateChangedEvent>(RefRelayActionEvent);
        SubscribeLocalEvent<ActionsComponent, GhostAttemptEvent>(RefRelayActionEvent);
    }

    private void RefRelayActionEvent<T>(EntityUid uid, ActionsComponent component, ref T args) where T : struct
    {
        RelayEvent((uid, component), ref args);
    }

    private void RelayActionEvent<T>(EntityUid uid, ActionsComponent component, T args) where T : class
    {
        RelayEvent((uid, component), args);
    }

    public void RelayEvent<T>(Entity<ActionsComponent> ent, ref T args) where T : struct
    {
        // this copies the by-ref event if it is a struct
        var ev = new ActionRelayedEvent<T>(args);
        foreach (var action in ent.Comp.Actions)
        {
            RaiseLocalEvent(action, ref ev);
        }
        // and now we copy it back
        args = ev.Args;
    }

    public void RelayEvent<T>(Entity<ActionsComponent> ent, T args) where T : class
    {
        // this copies the by-ref event if it is a struct
        var ev = new ActionRelayedEvent<T>(args);
        foreach (var action in ent.Comp.Actions)
        {
            RaiseLocalEvent(action, ref ev);
        }
    }
}

/// <summary>
/// Event wrapper for relayed events.
/// </summary>
[ByRefEvent]
public record struct ActionRelayedEvent<TEvent>(TEvent Args)
{
    public TEvent Args = Args;
}
