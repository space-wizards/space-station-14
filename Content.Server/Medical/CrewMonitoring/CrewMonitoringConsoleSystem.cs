using System.Linq;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Pinpointer;
using Content.Shared.PowerCell;
using Robust.Server.GameObjects;

namespace Content.Server.Medical.CrewMonitoring;

public sealed partial class CrewMonitoringConsoleSystem : DevicePayloadSystem<CrewMonitoringConsoleComponent>
{
    [Dependency] private PowerCellSystem _cell = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<BroadcastSuitSensorStatePayload>(OnSuitSensorBroadcast);
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        component.ConnectedSensors.Clear();
    }

    private void OnSuitSensorBroadcast(Entity<CrewMonitoringConsoleComponent> ent, ref BroadcastSuitSensorStatePayload payload, ref DeviceNetworkPacketData args)
    {
        ent.Comp.ConnectedSensors = payload.SensorStatus;
        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Update all sensors info
        var allSensors = component.ConnectedSensors.Values.ToList();
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }
}
