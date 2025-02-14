using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Consoles;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Atmos.Monitor.Systems;

public sealed class AtmosAlertsComputerSystem : SharedAtmosAlertsComputerSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly AirAlarmSystem _airAlarmSystem = default!;
    [Dependency] private readonly AtmosDeviceNetworkSystem _atmosDevNet = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly NavMapSystem _navMapSystem = default!;
    [Dependency] private readonly DeviceListSystem _deviceListSystem = default!;

    private const float UpdateTime = 1.0f;

    // Note: this data does not need to be saved
    private float _updateTimer = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        // Console events
        SubscribeLocalEvent<AtmosAlertsComputerComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<AtmosAlertsComputerComponent, EntParentChangedMessage>(OnConsoleParentChanged);
        SubscribeLocalEvent<AtmosAlertsComputerComponent, AtmosAlertsComputerFocusChangeMessage>(OnFocusChangedMessage);

        // Grid events
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);

        // Alarm events
        SubscribeLocalEvent<AtmosAlertsDeviceComponent, EntityTerminatingEvent>(OnDeviceTerminatingEvent);
        SubscribeLocalEvent<AtmosAlertsDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchorChanged);
    }

    #region Event handling

    private void OnConsoleInit(EntityUid uid, AtmosAlertsComputerComponent component, ComponentInit args)
    {
        InitalizeConsole(uid, component);
    }

    private void OnConsoleParentChanged(EntityUid uid, AtmosAlertsComputerComponent component, EntParentChangedMessage args)
    {
        InitalizeConsole(uid, component);
    }

    private void OnFocusChangedMessage(EntityUid uid, AtmosAlertsComputerComponent component, AtmosAlertsComputerFocusChangeMessage args)
    {
        component.FocusDevice = args.FocusDevice;
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        // Collect grids
        var allGrids = args.NewGrids.ToList();

        if (!allGrids.Contains(args.Grid))
            allGrids.Add(args.Grid);

        // Update atmos monitoring consoles that stand upon an updated grid
        var query = AllEntityQuery<AtmosAlertsComputerComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (entXform.GridUid == null)
                continue;

            if (!allGrids.Contains(entXform.GridUid.Value))
                continue;

            InitalizeConsole(ent, entConsole);
        }
    }

    private void OnDeviceAnchorChanged(EntityUid uid, AtmosAlertsDeviceComponent component, AnchorStateChangedEvent args)
    {
        OnDeviceAdditionOrRemoval(uid, component, args.Anchored);
    }

    private void OnDeviceTerminatingEvent(EntityUid uid, AtmosAlertsDeviceComponent component, ref EntityTerminatingEvent args)
    {
        OnDeviceAdditionOrRemoval(uid, component, false);
    }

    private void OnDeviceAdditionOrRemoval(EntityUid uid, AtmosAlertsDeviceComponent component, bool isAdding)
    {
        var xform = Transform(uid);
        var gridUid = xform.GridUid;

        if (gridUid == null)
            return;

        if (!TryComp<NavMapComponent>(xform.GridUid, out var navMap))
            return;

        if (!TryGetAtmosDeviceNavMapData(uid, component, xform, out var data))
            return;

        var netEntity = GetNetEntity(uid);

        var query = AllEntityQuery<AtmosAlertsComputerComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (gridUid != entXform.GridUid)
                continue;

            if (isAdding)
            {
                entConsole.AtmosDevices.Add(data.Value);
            }

            else
            {
                entConsole.AtmosDevices.RemoveWhere(x => x.NetEntity == netEntity);
                _navMapSystem.RemoveNavMapRegion(gridUid.Value, navMap, netEntity);
            }

            Dirty(ent, entConsole);
        }
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            // Keep a list of UI entries for each gridUid, in case multiple consoles stand on the same grid
            var airAlarmEntriesForEachGrid = new Dictionary<EntityUid, AtmosAlertsComputerEntry[]>();
            var fireAlarmEntriesForEachGrid = new Dictionary<EntityUid, AtmosAlertsComputerEntry[]>();

            var query = AllEntityQuery<AtmosAlertsComputerComponent, TransformComponent>();
            while (query.MoveNext(out var ent, out var entConsole, out var entXform))
            {
                if (entXform?.GridUid == null)
                    continue;

                // Make a list of alarm state data for all the air and fire alarms on the grid
                if (!airAlarmEntriesForEachGrid.TryGetValue(entXform.GridUid.Value, out var airAlarmEntries))
                {
                    airAlarmEntries = GetAlarmStateData(entXform.GridUid.Value, AtmosAlertsComputerGroup.AirAlarm).ToArray();
                    airAlarmEntriesForEachGrid[entXform.GridUid.Value] = airAlarmEntries;
                }

                if (!fireAlarmEntriesForEachGrid.TryGetValue(entXform.GridUid.Value, out var fireAlarmEntries))
                {
                    fireAlarmEntries = GetAlarmStateData(entXform.GridUid.Value, AtmosAlertsComputerGroup.FireAlarm).ToArray();
                    fireAlarmEntriesForEachGrid[entXform.GridUid.Value] = fireAlarmEntries;
                }

                // Determine the highest level of alert for the console (based on non-silenced alarms)
                var highestAlert = AtmosAlarmType.Invalid;

                foreach (var entry in airAlarmEntries)
                {
                    if (entry.AlarmState > highestAlert && !entConsole.SilencedDevices.Contains(entry.NetEntity))
                        highestAlert = entry.AlarmState;
                }

                foreach (var entry in fireAlarmEntries)
                {
                    if (entry.AlarmState > highestAlert && !entConsole.SilencedDevices.Contains(entry.NetEntity))
                        highestAlert = entry.AlarmState;
                }

                // Update the appearance of the console based on the highest recorded level of alert
                if (TryComp<AppearanceComponent>(ent, out var entAppearance))
                    _appearance.SetData(ent, AtmosAlertsComputerVisuals.ComputerLayerScreen, (int) highestAlert, entAppearance);

                // If the console UI is open, send UI data to each subscribed session
                UpdateUIState(ent, airAlarmEntries, fireAlarmEntries, entConsole, entXform);
            }
        }
    }

    public void UpdateUIState
        (EntityUid uid,
        AtmosAlertsComputerEntry[] airAlarmStateData,
        AtmosAlertsComputerEntry[] fireAlarmStateData,
        AtmosAlertsComputerComponent component,
        TransformComponent xform)
    {
        if (!_userInterfaceSystem.IsUiOpen(uid, AtmosAlertsComputerUiKey.Key))
            return;

        var gridUid = xform.GridUid!.Value;

        if (!HasComp<MapGridComponent>(gridUid))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        EnsureComp<NavMapComponent>(gridUid);

        // Gathering remaining data to be send to the client
        var focusAlarmData = GetFocusAlarmData(uid, GetEntity(component.FocusDevice), gridUid);

        // Set the UI state
        _userInterfaceSystem.SetUiState(uid, AtmosAlertsComputerUiKey.Key,
            new AtmosAlertsComputerBoundInterfaceState(airAlarmStateData, fireAlarmStateData, focusAlarmData));
    }

    private List<AtmosAlertsComputerEntry> GetAlarmStateData(EntityUid gridUid, AtmosAlertsComputerGroup group)
    {
        var alarmStateData = new List<AtmosAlertsComputerEntry>();

        var queryAlarms = AllEntityQuery<AtmosAlertsDeviceComponent, AtmosAlarmableComponent, DeviceNetworkComponent, TransformComponent>();
        while (queryAlarms.MoveNext(out var ent, out var entDevice, out var entAtmosAlarmable, out var entDeviceNetwork, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            if (!entXform.Anchored)
                continue;

            if (entDevice.Group != group)
                continue;

            if (!TryComp<MapGridComponent>(entXform.GridUid, out var mapGrid))
                continue;

            if (!TryComp<NavMapComponent>(entXform.GridUid, out var navMap))
                continue;

            // If emagged, change the alarm type to normal
            var alarmState = (entAtmosAlarmable.LastAlarmState == AtmosAlarmType.Emagged) ? AtmosAlarmType.Normal : entAtmosAlarmable.LastAlarmState;

            // Unpowered alarms can't sound
            if (TryComp<ApcPowerReceiverComponent>(ent, out var entAPCPower) && !entAPCPower.Powered)
                alarmState = AtmosAlarmType.Invalid;

            // Create entry
            var netEnt = GetNetEntity(ent);

            var entry = new AtmosAlertsComputerEntry
                (netEnt,
                GetNetCoordinates(entXform.Coordinates),
                entDevice.Group,
                alarmState,
                MetaData(ent).EntityName,
                entDeviceNetwork.Address);

            // Get the list of sensors attached to the alarm
            var sensorList = TryComp<DeviceListComponent>(ent, out var entDeviceList) ? _deviceListSystem.GetDeviceList(ent, entDeviceList) : null;

            if (sensorList?.Any() == true)
            {
                var alarmRegionSeeds = new HashSet<Vector2i>();

                // If valid and anchored, use the position of sensors as seeds for the region
                foreach (var (address, sensorEnt) in sensorList)
                {
                    if (!sensorEnt.IsValid() || !HasComp<AtmosMonitorComponent>(sensorEnt))
                        continue;

                    var sensorXform = Transform(sensorEnt);

                    if (sensorXform.Anchored && sensorXform.GridUid == entXform.GridUid)
                        alarmRegionSeeds.Add(_mapSystem.CoordinatesToTile(entXform.GridUid.Value, mapGrid, _transformSystem.GetMapCoordinates(sensorEnt, sensorXform)));
                }

                var regionProperties = new SharedNavMapSystem.NavMapRegionProperties(netEnt, AtmosAlertsComputerUiKey.Key, alarmRegionSeeds);
                _navMapSystem.AddOrUpdateNavMapRegion(gridUid, navMap, netEnt, regionProperties);
            }

            else
            {
                _navMapSystem.RemoveNavMapRegion(entXform.GridUid.Value, navMap, netEnt);
            }

            alarmStateData.Add(entry);
        }

        return alarmStateData;
    }

    private AtmosAlertsFocusDeviceData? GetFocusAlarmData(EntityUid uid, EntityUid? focusDevice, EntityUid gridUid)
    {
        if (focusDevice == null)
            return null;

        var focusDeviceXform = Transform(focusDevice.Value);

        if (!focusDeviceXform.Anchored ||
            focusDeviceXform.GridUid != gridUid ||
            !TryComp<AirAlarmComponent>(focusDevice.Value, out var focusDeviceAirAlarm))
        {
            return null;
        }

        // Force update the sensors attached to the alarm
        if (!_userInterfaceSystem.IsUiOpen(focusDevice.Value, SharedAirAlarmInterfaceKey.Key))
        {
            _atmosDevNet.Register(focusDevice.Value, null);
            _atmosDevNet.Sync(focusDevice.Value, null);

            foreach ((var address, var _) in focusDeviceAirAlarm.SensorData)
                _atmosDevNet.Register(uid, null);
        }

        // Get the sensor data
        var temperatureData = (_airAlarmSystem.CalculateTemperatureAverage(focusDeviceAirAlarm), AtmosAlarmType.Normal);
        var pressureData = (_airAlarmSystem.CalculatePressureAverage(focusDeviceAirAlarm), AtmosAlarmType.Normal);
        var gasData = new Dictionary<Gas, (float, float, AtmosAlarmType)>();

        foreach ((var address, var sensorData) in focusDeviceAirAlarm.SensorData)
        {
            if (sensorData.TemperatureThreshold.CheckThreshold(sensorData.Temperature, out var temperatureState) &&
                (int) temperatureState > (int) temperatureData.Item2)
            {
                temperatureData = (temperatureData.Item1, temperatureState);
            }

            if (sensorData.PressureThreshold.CheckThreshold(sensorData.Pressure, out var pressureState) &&
                (int) pressureState > (int) pressureData.Item2)
            {
                pressureData = (pressureData.Item1, pressureState);
            }

            if (focusDeviceAirAlarm.SensorData.Sum(g => g.Value.TotalMoles) > 1e-8)
            {
                foreach ((var gas, var threshold) in sensorData.GasThresholds)
                {
                    if (!gasData.ContainsKey(gas))
                    {
                        float mol = _airAlarmSystem.CalculateGasMolarConcentrationAverage(focusDeviceAirAlarm, gas, out var percentage);

                        if (mol < 1e-8)
                            continue;

                        gasData[gas] = (mol, percentage, AtmosAlarmType.Normal);
                    }

                    if (threshold.CheckThreshold(gasData[gas].Item2, out var gasState) &&
                        (int) gasState > (int) gasData[gas].Item3)
                    {
                        gasData[gas] = (gasData[gas].Item1, gasData[gas].Item2, gasState);
                    }
                }
            }
        }

        return new AtmosAlertsFocusDeviceData(GetNetEntity(focusDevice.Value), temperatureData, pressureData, gasData);
    }

    private HashSet<AtmosAlertsDeviceNavMapData> GetAllAtmosDeviceNavMapData(EntityUid gridUid)
    {
        var atmosDeviceNavMapData = new HashSet<AtmosAlertsDeviceNavMapData>();

        var query = AllEntityQuery<AtmosAlertsDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entComponent, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            if (TryGetAtmosDeviceNavMapData(ent, entComponent, entXform, out var data))
                atmosDeviceNavMapData.Add(data.Value);
        }

        return atmosDeviceNavMapData;
    }

    private bool TryGetAtmosDeviceNavMapData
        (EntityUid uid,
        AtmosAlertsDeviceComponent component,
        TransformComponent xform,
        [NotNullWhen(true)] out AtmosAlertsDeviceNavMapData? output)
    {
        output = null;

        if (!xform.Anchored)
            return false;

        output = new AtmosAlertsDeviceNavMapData(GetNetEntity(uid), GetNetCoordinates(xform.Coordinates), component.Group);

        return true;
    }

    private void InitalizeConsole(EntityUid uid, AtmosAlertsComputerComponent component)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null)
            return;

        var grid = xform.GridUid.Value;
        component.AtmosDevices = GetAllAtmosDeviceNavMapData(grid);

        Dirty(uid, component);
    }
}
