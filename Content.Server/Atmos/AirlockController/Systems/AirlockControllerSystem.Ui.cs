using System.Linq;
using Content.Server.Atmos.AirlockController.Components;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Doors.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos.AirlockController;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Doors;
using Content.Shared.Interaction;

namespace Content.Server.Atmos.AirlockController.Systems;

/// <summary>
///     Two windows: Main controller and config window.
///     Separate so we only send data if config itself is open.
/// </summary>
public sealed partial class AirlockControllerSystem
{
    private void InitializeUi()
    {

        Subs.BuiEvents<AirlockControllerComponent>(AirlockControllerUiKey.Key,
            subs =>
        {
            subs.Event<AirlockControllerCycleMessage>(OnCycleMessage);
            subs.Event<AirlockControllerCancelMessage>(OnCancelMessage);
            subs.Event<AirlockControllerOpenConfigMessage>(OnOpenConfigMessage);
        });

        // The rest of the config messages are predicted, they are in shared
        Subs.BuiEvents<AirlockControllerComponent>(AirlockControllerUiKey.Config,
            subs =>
        {
            subs.Event<AirlockControllerForceSideMessage>(OnForceSide);
            subs.Event<BoundUIClosedEvent>(OnConfigClosed);
        });
    }

    #region Shared hooks

    protected override void UpdateUi(Entity<AirlockControllerComponent> ent)
    {
        var state = new AirlockControllerUiState
        {
            Status = GetStatus(ent),
            CancelRequested = ent.Comp.CancelRequested,
            ChamberPressure = TryGetChamberPressure(ent, out var reading) ? reading.Mean : null,
        };

        UserInterfaceSystem.SetUiState(ent.Owner, AirlockControllerUiKey.Key, state);

        if (UserInterfaceSystem.IsUiOpen(ent.Owner, AirlockControllerUiKey.Config))
            UpdateConfigUi(ent);
    }

    protected override bool CanEdit(Entity<AirlockControllerComponent> ent, EntityUid actor)
    {
        if (!CheckConfigAccess(ent, actor))
            return false;

        // One entry covers the whole time the window is open
        if (ent.Comp.LoggedEditors.Add(actor))
            LogUse(ent, actor);

        return true;
    }

    protected override bool IsValidDevice(
        Entity<AirlockControllerComponent> ent,
        EntityUid device,
        AirlockDeviceKind kind,
        EntityUid actor)
    {
        if (!_deviceList.GetAllDevices(ent.Owner).Contains(device))
            return false;

        return kind switch
        {
            AirlockDeviceKind.Vent => _ventQuery.HasComp(device) || _scrubberQuery.HasComp(device),
            AirlockDeviceKind.Door => _doorQuery.HasComp(device) && CanCommandDoor(ent, device, actor),
            AirlockDeviceKind.Sensor => _sensorQuery.HasComp(device),
            AirlockDeviceKind.Cycler => _cyclerQuery.HasComp(device),
            _ => false,
        };
    }

    /// <summary>
    ///     Atmos access to edit controller is different from door access!
    /// </summary>
    private bool CanCommandDoor(Entity<AirlockControllerComponent> ent, EntityUid door, EntityUid actor)
    {
        if (IsMapping(ent) || IsAllowedQuiet(actor, door))
            return true;

        DenyAccess(ent, actor);
        return false;
    }

    protected override void OnDoorAssigned(Entity<AirlockControllerComponent> ent, EntityUid door)
    {
        if (_netQuery.TryComp(door, out var net) && !string.IsNullOrEmpty(net.Address))
            SendDoorCommand(ent, door, net.Address, DoorNetworkCommands.Sync);
    }

    protected override void OnDoorUnassigned(Entity<AirlockControllerComponent> ent, EntityUid door)
    {
        if (_netQuery.TryComp(door, out var net))
            ent.Comp.DoorReports.Remove(net.Address);
    }

    protected override void OnCyclerUnassigned(Entity<AirlockControllerComponent> ent, EntityUid cycler)
    {
        // The panel gets display from here
        if (_cyclerQuery.TryComp(cycler, out var panel))
            panel.Controller = null;
    }

    protected override void OnMaintenanceChanged(Entity<AirlockControllerComponent> ent)
    {
        ApplyMaintenanceMode(ent);
    }

    #endregion

    [SubscribeLocalEvent]
    private void OnActivate(Entity<AirlockControllerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex || args.Handled)
            return;

        // Mappers get the config, regular UI is useless to them
        if (IsMapping(ent))
        {
            UserInterfaceSystem.OpenUi(ent.Owner, AirlockControllerUiKey.Config, args.User);
            UpdateConfigUi(ent);
            args.Handled = true;
            return;
        }

        if (!this.IsPowered(ent, EntityManager))
            return;

        UserInterfaceSystem.OpenUi(ent.Owner, AirlockControllerUiKey.Key, args.User);
        UpdateUi(ent);
        args.Handled = true;
    }

    #region Status window

    private void OnCycleMessage(Entity<AirlockControllerComponent> ent, ref AirlockControllerCycleMessage args)
    {
        if (!IsValidSide(args.Side))
            return;

        TryRequestCycle(ent, args.Side, args.Actor);
    }

    private void OnCancelMessage(Entity<AirlockControllerComponent> ent, ref AirlockControllerCancelMessage args)
    {
        RequestCancel(ent);
        UpdateUi(ent);
    }

    private void OnOpenConfigMessage(Entity<AirlockControllerComponent> ent, ref AirlockControllerOpenConfigMessage args)
    {
        if (!CheckConfigAccess(ent, args.Actor))
            return;

        UserInterfaceSystem.OpenUi(ent.Owner, AirlockControllerUiKey.Config, args.Actor);
        UpdateConfigUi(ent);
    }

    #endregion

    #region Config window

    private void OnConfigClosed(Entity<AirlockControllerComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.LoggedEditors.Remove(args.Actor);
    }

    private bool CheckConfigAccess(Entity<AirlockControllerComponent> ent, EntityUid user)
    {
        if (IsMapping(ent))
            return true;

        if (!IsAllowedQuiet(user, ent))
        {
            DenyAccess(ent, user);
            return false;
        }

        return true;
    }

    private void UpdateConfigUi(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;
        var devices = _deviceList.GetDeviceList(ent.Owner);
        var entries = new List<AirlockDeviceEntry>();
        var sensors = new List<AirlockSensorOption>();
        var chamberSensors = 0;

        foreach (var (address, uid) in devices)
        {
            var entry = new AirlockDeviceEntry
            {
                Device = GetNetEntity(uid),
                Address = address,
                Name = Name(uid),
                // Components because if we go for addresses saving and such breaks, thanks NetworkDevicebama
                IsVent = _ventQuery.HasComp(uid),
                IsScrubber = _scrubberQuery.HasComp(uid),
                IsSensor = _sensorQuery.HasComp(uid),
                IsDoor = _doorQuery.HasComp(uid),
                IsCycler = _cyclerQuery.HasComp(uid),
            };

            // Vents inherently always inside
            if (entry.IsSensor && !entry.IsVent && !entry.IsScrubber)
            {
                sensors.Add(new AirlockSensorOption
                {
                    Device = entry.Device,
                    Name = $"{entry.Name} ({address})",
                });
            }

            // Whatever isn't watching a side is watching the chamber
            if (entry.IsSensor && !comp.TargetSensors.ContainsValue(uid))
                chamberSensors++;

            entries.Add(entry);
        }

        sensors.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        entries.Sort((a, b) =>
        {
            var group = DeviceGroup(a).CompareTo(DeviceGroup(b));
            return group != 0 ? group : string.CompareOrdinal(a.Name, b.Name);
        });

        UserInterfaceSystem.SetUiState(ent.Owner, AirlockControllerUiKey.Config, new AirlockControllerConfigUiState
        {
            Devices = entries,
            Address = _netQuery.CompOrNull(ent)?.Address ?? string.Empty,
            DoorCount = BoundDoors(ent).Count,
            ChamberSensorCount = chamberSensors,
            CurrentSide = comp.CurrentSide,
            Sensors = sensors,
            TargetPressureA = SensorReading(comp, AirlockSide.A),
            TargetPressureB = SensorReading(comp, AirlockSide.B),
        });
    }

    /// <summary>
    ///     For UI sorting devices in neat groups
    /// </summary>
    private static int DeviceGroup(AirlockDeviceEntry device)
    {
        if (device.IsDoor)
            return 0;

        if (device.IsCycler)
            return 1;

        if (device.IsVent)
            return 2;

        if (device.IsScrubber)
            return 3;

        return device.IsSensor ? 4 : 5;
    }

    private float? SensorReading(AirlockControllerComponent comp, AirlockSide side)
    {
        return comp.TargetSensors.TryGetValue(side, out var sensor)
               && _netQuery.TryComp(sensor, out var net)
               && comp.SensorData.TryGetValue(net.Address, out var data)
            ? data.Pressure
            : null;
    }

    private void OnForceSide(Entity<AirlockControllerComponent> ent, ref AirlockControllerForceSideMessage args)
    {
        if (!CanEdit(ent, args.Actor))
            return;

        if (!IsValidSide(args.Side))
            return;

        ForceSide(ent, args.Side);
    }

    #endregion
}
