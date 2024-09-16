using Content.Server.Atmos.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceNetwork.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Consoles;
using Content.Shared.Labels.Components;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Atmos.Consoles;

public sealed class AtmosMonitoringConsoleSystem : SharedAtmosMonitoringConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;
    [Dependency] private readonly AirAlarmSystem _airAlarmSystem = default!;
    [Dependency] private readonly AtmosDeviceNetworkSystem _atmosDevNet = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    // Private variables
    // Note: this data does not need to be saved
    private Dictionary<EntityUid, Dictionary<Vector2i, AtmosPipeChunk>> _gridAtmosPipeChunks = new();
    private float _updateTimer = 1.0f;

    // Constants
    private const float UpdateTime = 1.0f;
    private const int ChunkSize = 4;

    public override void Initialize()
    {
        base.Initialize();

        // Console events
        SubscribeLocalEvent<AtmosMonitoringConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<AtmosMonitoringConsoleComponent, AnchorStateChangedEvent>(OnConsoleAnchorChanged);
        SubscribeLocalEvent<AtmosMonitoringConsoleComponent, EntParentChangedMessage>(OnConsoleParentChanged);

        // Tracked device events
        SubscribeLocalEvent<AtmosMonitoringConsoleDeviceComponent, ComponentInit>(OnConsoleDeviceInit);
        SubscribeLocalEvent<AtmosMonitoringConsoleDeviceComponent, AnchorStateChangedEvent>(OnConsoleDeviceAnchorChanged);
        SubscribeLocalEvent<AtmosMonitoringConsoleDeviceComponent, NodeGroupsRebuilt>(OnConsoleDeviceNodeGroupsRebuilt);

        // Grid events
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);

        // Pipe events
        SubscribeLocalEvent<AtmosPipeColorComponent, AtmosPipeColorChangedEvent>(OnPipeColorChanged);
        SubscribeLocalEvent<AtmosPipeColorComponent, NodeGroupsRebuilt>(OnPipeNodeGroupsChanged);
    }

    #region Event handling

    private void OnConsoleInit(EntityUid uid, AtmosMonitoringConsoleComponent component, ComponentInit args)
    {
        InitializeAtmosMonitoringConsole(uid, component);
    }

    private void OnConsoleAnchorChanged(EntityUid uid, AtmosMonitoringConsoleComponent component, AnchorStateChangedEvent args)
    {
        InitializeAtmosMonitoringConsole(uid, component);
    }

    private void OnConsoleParentChanged(EntityUid uid, AtmosMonitoringConsoleComponent component, EntParentChangedMessage args)
    {
        InitializeAtmosMonitoringConsole(uid, component);
    }

    private void OnConsoleDeviceInit(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component, ComponentInit args)
    {
        InitializeAtmosMonitoringDevice(uid, component);
    }

    private void OnConsoleDeviceAnchorChanged(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component, AnchorStateChangedEvent args)
    {
        InitializeAtmosMonitoringDevice(uid, component);
    }

    private void OnConsoleDeviceNodeGroupsRebuilt(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component, NodeGroupsRebuilt args)
    {
        InitializeAtmosMonitoringDevice(uid, component);
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        // Collect grids
        var allGrids = args.NewGrids.ToList();

        if (!allGrids.Contains(args.Grid))
            allGrids.Add(args.Grid);

        // Rebuild the pipe networks on the affected grids
        foreach (var ent in allGrids)
        {
            if (!TryComp<MapGridComponent>(ent, out var grid))
                continue;

            RebuildAtmosPipeGrid(ent, grid);
        }

        // Update atmos monitoring consoles that stand upon an updated grid
        var query = AllEntityQuery<AtmosMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (entXform.GridUid == null)
                continue;

            if (!allGrids.Contains(entXform.GridUid.Value))
                continue;

            InitializeAtmosMonitoringConsole(ent, entConsole);
        }
    }

    private void OnPipeColorChanged(EntityUid uid, AtmosPipeColorComponent component, ref AtmosPipeColorChangedEvent args)
    {
        OnPipeChange(uid);
    }

    private void OnPipeNodeGroupsChanged(EntityUid uid, AtmosPipeColorComponent component, NodeGroupsRebuilt args)
    {
        OnPipeChange(uid);
    }

    private void OnPipeChange(EntityUid uid)
    {
        var xform = Transform(uid);
        var gridUid = xform.GridUid;

        if (gridUid == null)
            return;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        RebuildSingleTileOfPipeNetwork(gridUid.Value, grid, xform.Coordinates);
    }

    #endregion

    #region UI updates

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = AllEntityQuery<AtmosMonitoringConsoleComponent, TransformComponent>();
            while (query.MoveNext(out var ent, out var entConsole, out var entXform))
            {
                if (entXform?.GridUid == null)
                    continue;

                UpdateUIState(ent, entConsole, entXform);
            }
        }
    }

    public void UpdateUIState
        (EntityUid uid,
        AtmosMonitoringConsoleComponent component,
        TransformComponent xform)
    {
        if (!_userInterfaceSystem.IsUiOpen(uid, AtmosMonitoringConsoleUiKey.Key))
            return;

        var gridUid = xform.GridUid!.Value;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        if (!TryComp<GridAtmosphereComponent>(gridUid, out var atmosphere))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        EnsureComp<NavMapComponent>(gridUid);

        // Gathering remaining data to be send to the client
        var atmosNetworks = new List<AtmosMonitoringConsoleEntry>();

        var query = AllEntityQuery<GasPipeSensorComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entSensor, out var entXform))
        {
            if (entXform?.GridUid != xform.GridUid)
                continue;

            var entry = CreateAtmosMonitoringConsoleEntry(ent, entXform);

            if (entry != null)
                atmosNetworks.Add(entry.Value);
        }

        // Set the UI state
        _userInterfaceSystem.SetUiState(uid, AtmosMonitoringConsoleUiKey.Key,
            new AtmosMonitoringConsoleBoundInterfaceState(atmosNetworks.ToArray()));
    }

    private AtmosMonitoringConsoleEntry? CreateAtmosMonitoringConsoleEntry(EntityUid uid, TransformComponent xform)
    {
        AtmosMonitoringConsoleEntry? entry = null;

        var netEnt = GetNetEntity(uid);
        var name = MetaData(uid).EntityName;
        var address = string.Empty;

        if (xform.GridUid == null)
            return null;

        if (!TryGettingFirstPipeNode(uid, out var pipeNode, out var netId) ||
            pipeNode == null ||
            netId == null)
            return null;

        // Name device based on its label, if available
        if (TryComp<LabelComponent>(uid, out var label) && label.CurrentLabel != null)
            name = label.CurrentLabel;

        // Otherwise use its base name and network address
        else if (TryComp<DeviceNetworkComponent>(uid, out var deviceNet))
            address = deviceNet.Address;

        // Unpowered device entry
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver) && !apcPowerReceiver.Powered)
        {
            entry = new AtmosMonitoringConsoleEntry
                (netEnt, GetNetCoordinates(xform.Coordinates), AtmosMonitoringConsoleGroup.GasPipeSensor, netId.Value, name, address)
            {
                IsPowered = false
            };

            return entry;
        }

        var gasData = new Dictionary<Gas, float>();
        var isAirPresent = pipeNode.Air.TotalMoles > 0;

        if (isAirPresent)
        {
            foreach (var gas in Enum.GetValues<Gas>())
            {
                if (pipeNode.Air[(int)gas] > 0)
                    gasData.Add(gas, pipeNode.Air[(int)gas] / pipeNode.Air.TotalMoles);
            }
        }

        entry = new AtmosMonitoringConsoleEntry
            (netEnt, GetNetCoordinates(xform.Coordinates), AtmosMonitoringConsoleGroup.GasPipeSensor, netId.Value, name, address)
        {
            TemperatureData = isAirPresent ? pipeNode.Air.Temperature : 0f,
            PressureData = pipeNode.Air.Pressure,
            TotalMolData = pipeNode.Air.TotalMoles,
            GasData = gasData,
        };

        return entry;
    }

    private HashSet<AtmosDeviceNavMapData> GetAllAtmosDeviceNavMapData(EntityUid gridUid)
    {
        var atmosDeviceNavMapData = new HashSet<AtmosDeviceNavMapData>();

        var query = AllEntityQuery<AtmosMonitoringConsoleDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entComponent, out var entXform))
        {
            if (TryGetAtmosDeviceNavMapData(ent, entComponent, entXform, gridUid, out var data))
                atmosDeviceNavMapData.Add(data.Value);
        }

        return atmosDeviceNavMapData;
    }

    private bool TryGetAtmosDeviceNavMapData
        (EntityUid uid,
        AtmosMonitoringConsoleDeviceComponent component,
        TransformComponent xform,
        EntityUid gridUid,
        [NotNullWhen(true)] out AtmosDeviceNavMapData? device)
    {
        device = null;

        if (xform.GridUid != gridUid)
            return false;

        if (!xform.Anchored)
            return false;

        var direction = xform.LocalRotation.GetCardinalDir();
        Color? color = null;

        if (TryComp<AtmosPipeColorComponent>(uid, out var atmosPipeColor))
            color = atmosPipeColor.Color;

        if (!TryGettingFirstPipeNode(uid, out var pipeNode, out var netId))
            netId = -1;

        device = new AtmosDeviceNavMapData(GetNetEntity(uid), GetNetCoordinates(xform.Coordinates), component.Group, netId.Value, color, direction);

        return true;
    }

    #endregion

    #region Pipe net functions

    private Dictionary<Vector2i, AtmosPipeChunk> RebuildAtmosPipeGrid(EntityUid gridUid, MapGridComponent grid)
    {
        // Clears all chunks for the associated grid
        var allChunks = new Dictionary<Vector2i, AtmosPipeChunk>();
        _gridAtmosPipeChunks[gridUid] = allChunks;

        // Adds all atmos pipe to the grid
        var query = AllEntityQuery<AtmosPipeColorComponent, NodeContainerComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entAtmosPipeColor, out var entNodeContainer, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            if (!entXform.Anchored)
                continue;

            var tile = _sharedMapSystem.GetTileRef(gridUid, grid, entXform.Coordinates);
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, ChunkSize);
            var relative = SharedMapSystem.GetChunkRelative(tile.GridIndices, ChunkSize);

            if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
                chunk = new AtmosPipeChunk(chunkOrigin);

            UpdateAtmosPipeChunk(ent, entNodeContainer, entAtmosPipeColor, GetTileIndex(relative), ref chunk);

            allChunks[chunkOrigin] = chunk;
        }

        return allChunks;
    }

    private void RebuildSingleTileOfPipeNetwork(EntityUid gridUid, MapGridComponent grid, EntityCoordinates coords)
    {
        if (!_gridAtmosPipeChunks.TryGetValue(gridUid, out var allChunks))
            allChunks = new Dictionary<Vector2i, AtmosPipeChunk>();

        var tile = _sharedMapSystem.GetTileRef(gridUid, grid, coords);
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, ChunkSize);
        var relative = SharedMapSystem.GetChunkRelative(tile.GridIndices, ChunkSize);
        var tileIdx = GetTileIndex(relative);

        if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
            chunk = new AtmosPipeChunk(chunkOrigin);

        // Remove all stale values for the tile
        foreach (var (index, atmosPipeData) in chunk.AtmosPipeData)
        {
            var mask = (ulong)SharedNavMapSystem.AllDirMask << tileIdx * SharedNavMapSystem.Directions;
            chunk.AtmosPipeData[index] = atmosPipeData & ~mask;
        }

        // Rebuild the pipe data tile
        foreach (var ent in _sharedMapSystem.GetAnchoredEntities(gridUid, grid, coords))
        {
            if (!TryComp<AtmosPipeColorComponent>(ent, out var entAtmosPipeColor))
                continue;

            if (!TryComp<NodeContainerComponent>(ent, out var entNodeContainer))
                continue;

            UpdateAtmosPipeChunk(ent, entNodeContainer, entAtmosPipeColor, tileIdx, ref chunk);
        }

        allChunks[chunkOrigin] = chunk;

        // Update the components of the monitoring consoles that are attached to the same grid
        var query = AllEntityQuery<AtmosMonitoringConsoleComponent, TransformComponent>();

        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (gridUid != entXform.GridUid)
                continue;

            entConsole.AtmosPipeChunks = allChunks;
            Dirty(ent, entConsole);
        }
    }

    private void UpdateAtmosPipeChunk(EntityUid uid, NodeContainerComponent nodeContainer, AtmosPipeColorComponent pipeColor, int tileIdx, ref AtmosPipeChunk chunk)
    {
        foreach ((var id, var node) in nodeContainer.Nodes)
        {
            if (node is not PipeNode)
                continue;

            var pipeNode = (PipeNode)node;
            var netId = GetPipeNodeNetId(pipeNode);
            var pipeDirection = pipeNode.CurrentPipeDirection;

            chunk.AtmosPipeData.TryGetValue((netId, pipeColor.Color.ToHex()), out var atmosPipeData);
            atmosPipeData |= (ulong)pipeDirection << tileIdx * SharedNavMapSystem.Directions;
            chunk.AtmosPipeData[(netId, pipeColor.Color.ToHex())] = atmosPipeData;
        }
    }

    private bool TryGettingFirstPipeNode(EntityUid uid, [NotNullWhen(true)] out PipeNode? pipeNode, [NotNullWhen(true)] out int? netId)
    {
        pipeNode = null;
        netId = null;

        if (!TryComp<NodeContainerComponent>(uid, out var nodeContainer))
            return false;

        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node is PipeNode)
            {
                pipeNode = (PipeNode)node;
                netId = GetPipeNodeNetId(pipeNode);

                return true;
            }
        }

        return false;
    }

    private int GetPipeNodeNetId(PipeNode pipeNode)
    {
        if (pipeNode.NodeGroup is BaseNodeGroup)
        {
            var nodeGroup = (BaseNodeGroup)pipeNode.NodeGroup;

            return nodeGroup.NetId;
        }

        return -1;
    }

    #endregion

    #region Initialization functions

    private void InitializeAtmosMonitoringConsole(EntityUid uid, AtmosMonitoringConsoleComponent component)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null)
            return;

        var grid = xform.GridUid.Value;

        if (!TryComp<MapGridComponent>(grid, out var map))
            return;

        if (!_gridAtmosPipeChunks.TryGetValue(grid, out var allChunks))
            allChunks = RebuildAtmosPipeGrid(grid, map);

        component.AtmosPipeChunks = allChunks;
        component.AtmosDevices = GetAllAtmosDeviceNavMapData(grid);

        Dirty(uid, component);
    }

    private void InitializeAtmosMonitoringDevice(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component)
    {
        var xform = Transform(uid);
        var gridUid = xform.GridUid;

        if (gridUid == null)
            return;

        var netEntity = EntityManager.GetNetEntity(uid);

        var query = AllEntityQuery<AtmosMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (gridUid != entXform.GridUid)
                continue;

            entConsole.AtmosDevices.RemoveWhere(x => x.NetEntity == netEntity);

            if (xform.Anchored)
            {
                if (TryGetAtmosDeviceNavMapData(uid, component, xform, gridUid.Value, out var data))
                    entConsole.AtmosDevices.Add(data.Value);
            }

            Dirty(ent, entConsole);
        }
    }

    #endregion

    private int GetTileIndex(Vector2i relativeTile)
    {
        return relativeTile.X * ChunkSize + relativeTile.Y;
    }
}
