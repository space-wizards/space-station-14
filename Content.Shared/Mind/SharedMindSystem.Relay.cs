using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Mind.Components;

namespace Content.Shared.Mind;

/// <summary>
///     Relays events raised on a mobs body to its mind and mind role entities.
///     Useful for events that should be raised both on the body and the mind.
/// </summary>
public abstract partial class SharedMindSystem : EntitySystem
{
    public void InitializeRelay()
    {
        // for name modifiers that depend on certain mind roles
        SubscribeLocalEvent<MindContainerComponent, RefreshNameModifiersEvent>(RelayRefToMind);
    }

    protected void RelayToMind<T>(EntityUid uid, MindContainerComponent component, T args) where T : class
    {
        var ev = new MindRelayedEvent<T>(args);

        if (TryGetMind(uid, out var mindId, out var mindComp, component))
        {
            RaiseLocalEvent(mindId, ref ev);

            foreach (var role in mindComp.MindRoles)
                RaiseLocalEvent(role, ref ev);
        }
    }

    protected void RelayRefToMind<T>(EntityUid uid, MindContainerComponent component, ref T args) where T : class
    {
        var ev = new MindRelayedEvent<T>(args);

        if (TryGetMind(uid, out var mindId, out var mindComp, component))
        {
            RaiseLocalEvent(mindId, ref ev);

            foreach (var role in mindComp.MindRoles)
                RaiseLocalEvent(role, ref ev);
        }

        args = ev.Args;
    }
}

[ByRefEvent]
public record struct MindRelayedEvent<TEvent>(TEvent Args);
