using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GatewayStation.Components;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;

namespace Content.Server.GatewayStation.Systems;

public sealed class StationGatewayConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationGatewayConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<StationGatewayConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<StationGatewayConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnRemove(Entity<StationGatewayConsoleComponent> ent, ref ComponentRemove args)
    {
        //Clear data
    }

    private void OnPacketReceived(Entity<StationGatewayConsoleComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        //Update data

        UpdateUserInterface(ent);
    }

    private void OnUIOpened(Entity<StationGatewayConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void UpdateUserInterface(Entity<StationGatewayConsoleComponent> ent)
    {
        if (!_uiSystem.IsUiOpen(ent.Owner, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(ent);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        //Send data
    }
}
