using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Interaction;

namespace Content.Shared.CartridgeLoader;

public sealed partial class CartridgeLoaderSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<CartridgeLoaderComponent, AfterInteractEvent>(RelayEvent);
        SubscribeLocalEvent<CartridgeLoaderComponent, InteractUsingEvent>(RelayEvent);
        SubscribeLocalEvent<CartridgeLoaderComponent, DeviceNetworkPacketEvent>(RelayEvent);
    }

    private void RefRelayEvent<T>(EntityUid uid, CartridgeLoaderComponent component, ref T args) where T : struct
    {
        RefRelayEvent((uid, component), ref args);
    }

    private void RelayEvent<T>(EntityUid uid, CartridgeLoaderComponent component, T args) where T : class
    {
        RelayEvent((uid, component), args);
    }

    public void RefRelayEvent<T>(Entity<CartridgeLoaderComponent> ent, ref T args, bool foregroundOnly = false) where T : struct
    {
        var ev = new CartridgeRelayedEvent<T>(ent, args);
        if (foregroundOnly)
        {
            if (ent.Comp.ActiveProgram is { } foreground)
                RaiseLocalEvent(foreground, ref ev);
            args = ev.Args;
            return;
        }

        foreach (var program in GetAllPrograms(ent))
        {
            RaiseLocalEvent(program, ref ev);
        }
        args = ev.Args;
    }

    public void RelayEvent<T>(Entity<CartridgeLoaderComponent> ent, T args, bool foregroundOnly = false) where T : class
    {
        var ev = new CartridgeRelayedEvent<T>(ent, args);
        if (foregroundOnly)
        {
            if (ent.Comp.ActiveProgram is { } foreground)
                RaiseLocalEvent(foreground, ref ev);
            return;
        }

        foreach (var program in GetAllPrograms(ent))
        {
            RaiseLocalEvent(program, ref ev);
        }
    }
}

/// <summary>
/// Event wrapper for relayed events.
/// </summary>
[ByRefEvent]
public record struct CartridgeRelayedEvent<TEvent>(Entity<CartridgeLoaderComponent> Loader, TEvent Args);
