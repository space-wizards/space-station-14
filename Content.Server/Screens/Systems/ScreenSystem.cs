using Content.Shared.Screen;
using Content.Server.Screen.Components;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;


namespace Content.Server.Screen.Systems;

/// <summary>
/// Controls the wallmounted screens on stations and shuttles displaying e.g. FTL duration, ETA
/// </summary>
public sealed class ScreenSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScreenComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    /// <summary>
    /// Determines if/how a packet affects this screen.
    /// Currently there are 2 broadcast domains: Arrivals, and every other screen.
    /// Domain is determined by the <see cref="DeviceNetworkComponent.TransmitFrequency"/> on each timer.
    /// Each broadcast domain is divided into subnets. Screen MapUid determines subnet.
    /// So far I haven't needed more than per-map update granularity
    /// </summary>
    private void OnPacketReceived(EntityUid uid, ScreenComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(ScreenMasks.Updates, out ScreenUpdate[]? updates) || updates == null)
            return;

        // drop the packet if it's intended for a subnet (MapUid) that doesn't match our screen's
        var timerXform = Transform(uid);
        if (timerXform.MapUid == null)
            return;

        foreach (var update in updates)
            // the griduid check handled some null mapuid edge case involving hyperspace iirc
            if (TryGetEntity(update.Subnet, out var subnet) && (subnet == timerXform.MapUid || subnet == timerXform.GridUid))
                _appearanceSystem.SetData(uid, ScreenVisuals.Update, update);
    }
}
