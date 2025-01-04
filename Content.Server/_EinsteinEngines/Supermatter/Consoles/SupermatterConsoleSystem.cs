using Content.Server.Pinpointer;
using Content.Shared._EinsteinEngines.Supermatter.Components;
using Content.Shared._EinsteinEngines.Supermatter.Consoles;
using Content.Shared._EinsteinEngines.Supermatter.Monitor;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server._EinsteinEngines.Supermatter.Console.Systems;

public sealed class SupermatterConsoleSystem : SharedSupermatterConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NavMapSystem _navMapSystem = default!;

    private const float UpdateTime = 1.0f;

    // Note: this data does not need to be saved
    private float _updateTimer = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        // Console events
        SubscribeLocalEvent<SupermatterConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<SupermatterConsoleComponent, EntParentChangedMessage>(OnConsoleParentChanged);
        SubscribeLocalEvent<SupermatterConsoleComponent, SupermatterConsoleFocusChangeMessage>(OnFocusChangedMessage);

        // Grid events
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);

        // Alarm events
        SubscribeLocalEvent<SupermatterComponent, AnchorStateChangedEvent>(OnSupermatterAnchorChanged);
        SubscribeLocalEvent<SupermatterComponent, EntityTerminatingEvent>(OnSupermatterTerminatingEvent);
    }

    #region Event handling 

    private void OnConsoleInit(EntityUid uid, SupermatterConsoleComponent component, ComponentInit args)
    {
        InitalizeConsole(uid, component);
    }

    private void OnConsoleParentChanged(EntityUid uid, SupermatterConsoleComponent component, EntParentChangedMessage args)
    {
        InitalizeConsole(uid, component);
    }

    private void OnFocusChangedMessage(EntityUid uid, SupermatterConsoleComponent component, SupermatterConsoleFocusChangeMessage args)
    {
        component.FocusSupermatter = args.FocusSupermatter;
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        // Collect grids
        var allGrids = args.NewGrids.ToList();

        if (!allGrids.Contains(args.Grid))
            allGrids.Add(args.Grid);

        // Update supermatter monitoring consoles that stand upon an updated grid
        var query = AllEntityQuery<SupermatterConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (entXform.GridUid == null)
                continue;

            if (!allGrids.Contains(entXform.GridUid.Value))
                continue;

            InitalizeConsole(ent, entConsole);
        }
    }

    private void OnSupermatterAnchorChanged(EntityUid uid, SupermatterComponent component, AnchorStateChangedEvent args)
    {
        OnSupermatterAdditionOrRemoval(uid, component, args.Anchored);
    }

    private void OnSupermatterTerminatingEvent(EntityUid uid, SupermatterComponent component, ref EntityTerminatingEvent args)
    {
        OnSupermatterAdditionOrRemoval(uid, component, false);
    }

    private void OnSupermatterAdditionOrRemoval(EntityUid uid, SupermatterComponent component, bool isAdding)
    {
        var xform = Transform(uid);
        var gridUid = xform.GridUid;

        if (gridUid == null)
            return;

        if (!TryComp<NavMapComponent>(xform.GridUid, out var navMap))
            return;

        if (!TryGetSupermatterNavMapData(uid, component, xform, out var data))
            return;

        var netEntity = GetNetEntity(uid);

        var query = AllEntityQuery<SupermatterConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (gridUid != entXform.GridUid)
                continue;

            if (isAdding)
            {
                entConsole.Supermatters.Add(data.Value);
            }

            else
            {
                entConsole.Supermatters.RemoveWhere(x => x.NetEntity == netEntity);
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
            var supermatterEntriesForEachGrid = new Dictionary<EntityUid, SupermatterConsoleEntry[]>();

            var query = AllEntityQuery<SupermatterConsoleComponent, TransformComponent>();
            while (query.MoveNext(out var ent, out var entConsole, out var entXform))
            {
                if (entXform?.GridUid == null)
                    continue;

                // Make a list of alarm state data for all the supermatters on the grid
                if (!supermatterEntriesForEachGrid.TryGetValue(entXform.GridUid.Value, out var supermatterEntries))
                {
                    supermatterEntries = GetSupermatterStateData(entXform.GridUid.Value).ToArray();
                    supermatterEntriesForEachGrid[entXform.GridUid.Value] = supermatterEntries;
                }

                // Determine the highest level of status for the console
                var highestStatus = SupermatterStatusType.Inactive;

                foreach (var entry in supermatterEntries)
                {
                    var status = entry.EntityStatus;

                    if (status > highestStatus)
                        highestStatus = status;
                }

                // Update the appearance of the console based on the highest recorded level of alert
                if (TryComp<AppearanceComponent>(ent, out var entAppearance))
                    _appearance.SetData(ent, SupermatterConsoleVisuals.ComputerLayerScreen, (int)highestStatus, entAppearance);

                // If the console UI is open, send UI data to each subscribed session
                UpdateUIState(ent, supermatterEntries, entConsole, entXform);
            }
        }
    }

    public void UpdateUIState
        (EntityUid uid,
        SupermatterConsoleEntry[] supermatterStateData,
        SupermatterConsoleComponent component,
        TransformComponent xform)
    {
        if (!_userInterfaceSystem.IsUiOpen(uid, SupermatterConsoleUiKey.Key))
            return;

        var gridUid = xform.GridUid!.Value;

        if (!HasComp<MapGridComponent>(gridUid))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        EnsureComp<NavMapComponent>(gridUid);

        // Gathering remaining data to be send to the client
        var focusSupermatterData = GetFocusSupermatterData(uid, GetEntity(component.FocusSupermatter), gridUid);

        // Set the UI state
        _userInterfaceSystem.SetUiState(uid, SupermatterConsoleUiKey.Key,
            new SupermatterConsoleBoundInterfaceState(supermatterStateData, focusSupermatterData));
    }

    private List<SupermatterConsoleEntry> GetSupermatterStateData(EntityUid gridUid)
    {
        var supermatterStateData = new List<SupermatterConsoleEntry>();

        var querySupermatters = AllEntityQuery<SupermatterComponent, TransformComponent>();
        while (querySupermatters.MoveNext(out var ent, out var entSupermatter, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            if (!entXform.Anchored)
                continue;

            if (!TryComp<MapGridComponent>(entXform.GridUid, out var mapGrid))
                continue;

            if (!TryComp<NavMapComponent>(entXform.GridUid, out var navMap))
                continue;

            // Create entry
            var netEnt = GetNetEntity(ent);

            var entry = new SupermatterConsoleEntry
                (netEnt,
                GetNetCoordinates(entXform.Coordinates),
                MetaData(ent).EntityName,
                entSupermatter.Status);

            supermatterStateData.Add(entry);
        }

        return supermatterStateData;
    }

    private SupermatterFocusData? GetFocusSupermatterData(EntityUid uid, EntityUid? focusSupermatter, EntityUid gridUid)
    {
        if (focusSupermatter == null)
            return null;

        var focusSupermatterXform = Transform(focusSupermatter.Value);

        if (!focusSupermatterXform.Anchored ||
            focusSupermatterXform.GridUid != gridUid ||
            !TryComp<SupermatterComponent>(focusSupermatter.Value, out var focusComp))
        {
            return null;
        }

        // Get the sensor data
        /*var temperatureData = (_airAlarmSystem.CalculateTemperatureAverage(focusDeviceAirAlarm), AtmosAlarmType.Normal);
        var pressureData = (_airAlarmSystem.CalculatePressureAverage(focusDeviceAirAlarm), AtmosAlarmType.Normal);
        var gasData = new Dictionary<Gas, (float, float, AtmosAlarmType)>();

        foreach ((var address, var sensorData) in focusDeviceAirAlarm.SensorData)
        {
            if (sensorData.TemperatureThreshold.CheckThreshold(sensorData.Temperature, out var temperatureState) &&
                (int)temperatureState > (int)temperatureData.Item2)
            {
                temperatureData = (temperatureData.Item1, temperatureState);
            }

            if (sensorData.PressureThreshold.CheckThreshold(sensorData.Pressure, out var pressureState) &&
                (int)pressureState > (int)pressureData.Item2)
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
                        (int)gasState > (int)gasData[gas].Item3)
                    {
                        gasData[gas] = (gasData[gas].Item1, gasData[gas].Item2, gasState);
                    }
                }
            }
        }*/

        return new SupermatterFocusData(GetNetEntity(focusSupermatter.Value));
    }

    private HashSet<SupermatterNavMapData> GetAllSupermatterNavMapData(EntityUid gridUid)
    {
        var supermatterNavMapData = new HashSet<SupermatterNavMapData>();

        var query = AllEntityQuery<SupermatterComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entComponent, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            if (TryGetSupermatterNavMapData(ent, entComponent, entXform, out var data))
                supermatterNavMapData.Add(data.Value);
        }

        return supermatterNavMapData;
    }

    private bool TryGetSupermatterNavMapData
        (EntityUid uid,
        SupermatterComponent component,
        TransformComponent xform,
        [NotNullWhen(true)] out SupermatterNavMapData? output)
    {
        output = null;

        if (!xform.Anchored)
            return false;

        output = new SupermatterNavMapData(GetNetEntity(uid), GetNetCoordinates(xform.Coordinates));

        return true;
    }

    private void InitalizeConsole(EntityUid uid, SupermatterConsoleComponent component)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null)
            return;

        var grid = xform.GridUid.Value;
        component.Supermatters = GetAllSupermatterNavMapData(grid);

        Dirty(uid, component);
    }
}
