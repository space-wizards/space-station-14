using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.PowerCell;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        component.ConnectedSensors.Clear();
    }

    private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        // Check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;

        component.ConnectedSensors = sensorStatus;
        UpdateUserInterface(uid, component);
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

        if (!_uiSystem.TryGetUi(uid, CrewMonitoringUIKey.Key, out var bui))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Get all sensors info
        var allSensors = component.ConnectedSensors.Values.ToList();
        // The sensors we will be outputting to the monitor
        var outputSensors = new List<SuitSensorStatus>();

        foreach (var listing in allSensors)
        {
            // Check to ensure this listing is in one of our tracked departments or roles
            var isValidRole = false;
            foreach (var department in component.TrackedDepartments)
            {

                if (listing.JobDepartments.Contains(department) || department == "All")
                {
                    isValidRole = true;
                    break;
                }
            }
            foreach (var job in component.TrackedJobs)
            {
                if (listing.Job == job)
                {
                    isValidRole = true;
                    break;
                }
            }


            if (isValidRole)
            {
                // Only add conscious listings if we're supposed to
                var isValidListing = false;
                if (component.ShowConsciousListings)
                {
                    isValidListing = true;
                }
                else
                {
                    if (_mobState.IsCritical(uid) || _mobState.IsDead(uid) || _mobState.IsIncapacitated(uid))
                    {
                        isValidListing = true;
                    }
                }

                if (isValidListing)
                {
                    outputSensors.Add(listing);
                }
            }
        }

        _uiSystem.SetUiState(bui, new CrewMonitoringState(outputSensors));

    }
}
