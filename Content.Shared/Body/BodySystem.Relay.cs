using Content.Shared.Body.Events;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Medical;

namespace Content.Shared.Body;

public sealed partial class BodySystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(RefRelayBodyEvent);
        SubscribeLocalEvent<BodyComponent, TryVomitEvent>(RefRelayBodyEvent);
        SubscribeLocalEvent<BodyComponent, BeingGibbedEvent>(RefRelayBodyEvent);
        SubscribeLocalEvent<BodyComponent, ApplyOrganProfileDataEvent>(RefRelayBodyEvent);
        SubscribeLocalEvent<BodyComponent, ApplyOrganMarkingsEvent>(RefRelayBodyEvent);
        SubscribeLocalEvent<BodyComponent, OrganCopyAppearanceEvent>(RefRelayBodyEvent);
        SubscribeLocalEvent<BodyComponent, HumanoidLayerVisibilityChangedEvent>(RefRelayBodyEvent);
    }

    private void RefRelayBodyEvent<T>(EntityUid uid, BodyComponent component, ref T args) where T : struct
    {
        RelayEvent((uid, component), ref args);
    }

    private void RelayBodyEvent<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        RelayEvent((uid, component), args);
    }

    public void RelayEvent<T>(Entity<BodyComponent> ent, ref T args) where T : struct
    {
        var ev = new BodyRelayedEvent<T>(ent, args);
        foreach (var organ in ent.Comp.Organs?.ContainedEntities ?? [])
        {
            RaiseLocalEvent(organ, ref ev);
        }
        args = ev.Args;
    }

    public void RelayEvent<T>(Entity<BodyComponent> ent, T args) where T : class
    {
        var ev = new BodyRelayedEvent<T>(ent, args);
        foreach (var organ in ent.Comp.Organs?.ContainedEntities ?? [])
        {
            RaiseLocalEvent(organ, ref ev);
        }
    }
}

/// <summary>
/// Event wrapper for relayed events.
/// </summary>
[ByRefEvent]
public record struct BodyRelayedEvent<TEvent>(Entity<BodyComponent> Body, TEvent Args);
