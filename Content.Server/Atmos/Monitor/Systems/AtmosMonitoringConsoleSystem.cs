using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Labels.Components;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Atmos.Monitor.Systems;

public sealed class AtmosMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;
    [Dependency] private readonly AirAlarmSystem _airAlarmSystem = default!;
    [Dependency] private readonly AtmosDeviceNetworkSystem _atmosDevNet = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    // Note: this data does not need to be saved
    private Dictionary<EntityUid, Dictionary<Vector2i, AtmosPipeChunk>> _gridAtmosPipeChunks = new();
    private float _updateTimer = 1.0f;

    private const float UpdateTime = 1.0f;
    private EntityQuery<NodeContainerComponent> _nodeQuery;

    public static int ChunkSize = 4;

    public static int GetTileIndex(Vector2i relativeTile)
    {
        return relativeTile.X * ChunkSize + relativeTile.Y;
    }

    public override void Initialize()
    {
        base.Initialize();

        _nodeQuery = GetEntityQuery<NodeContainerComponent>();

        // Console events
        SubscribeLocalEvent<AtmosMonitoringConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<AtmosMonitoringConsoleComponent, EntParentChangedMessage>(OnConsoleParentChanged);

        // UI events
        SubscribeLocalEvent<AtmosMonitoringConsoleComponent, AtmosMonitoringConsoleFocusChangeMessage>(OnFocusChangedMessage);

        // Grid events
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
        SubscribeLocalEvent<AtmosPipeColorComponent, AtmosPipeColorChangedEvent>(OnPipeColorChanged);
        SubscribeLocalEvent<AtmosPipeColorComponent, NodeGroupsRebuilt>(OnPipeNodeGroupsChanged);
        SubscribeLocalEvent<AtmosMonitoringConsoleDeviceComponent, AnchorStateChangedEvent>(OnAtmosMonitoringConsoleDeviceAnchorChanged);
    }

    #region Event handling 

    private void OnConsoleInit(EntityUid uid, AtmosMonitoringConsoleComponent component, ComponentInit args)
    {
        InitalizeAtmosMonitoringConsole(uid, component);
    }

    private void OnConsoleParentChanged(EntityUid uid, AtmosMonitoringConsoleComponent component, EntParentChangedMessage args)
    {
        InitalizeAtmosMonitoringConsole(uid, component);
    }

    private void OnFocusChangedMessage(EntityUid uid, AtmosMonitoringConsoleComponent component, AtmosMonitoringConsoleFocusChangeMessage args)
    {
        component.FocusDevice = EntityManager.GetEntity(args.FocusDevice);
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

            InitalizeAtmosMonitoringConsole(ent, entConsole);
        }
    }

    private void OnPipeColorChanged(EntityUid uid, AtmosPipeColorComponent component, ref AtmosPipeColorChangedEvent args)
    {
        OnPipeChange(uid);
    }

    // TODO: split pipenet int arrays by nodegroup
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

    private void OnAtmosMonitoringConsoleDeviceAnchorChanged(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component, AnchorStateChangedEvent args)
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

            if (args.Anchored && TryGetAtmosDeviceNavMapData(uid, component, xform, gridUid.Value, out var data))
                entConsole.AtmosDevices.Add(data.Value);

            else if (!args.Anchored)
                entConsole.AtmosDevices.RemoveWhere(x => x.NetEntity == netEntity);
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
        AtmosFocusDeviceData? focusAlarmData = null;

        var query = AllEntityQuery<GasPipeSensorComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entSensor, out var entXform))
        {
            if (entXform?.GridUid != xform.GridUid)
                continue;

            var entry = CreateAtmosMonitoringConsoleEntry(uid, component, mapGrid, xform, ent, entXform, out var newFocusAlarmData);

            if (newFocusAlarmData != null)
                focusAlarmData = newFocusAlarmData;

            if (entry != null)
                atmosNetworks.Add(entry.Value);
        }

        if (component.FocusDevice != null &&
            TryGettingFirstPipeNode(component.FocusDevice.Value, Transform(component.FocusDevice.Value), mapGrid, out var pipeNode, out var netId) &&
            netId != component.FocusNetId)
        {
            component.FocusNetId = netId;
            Dirty(uid, component);
        }

        // Set the UI state
        _userInterfaceSystem.SetUiState(uid, AtmosMonitoringConsoleUiKey.Key,
            new AtmosMonitoringConsoleBoundInterfaceState(atmosNetworks.ToArray(), focusAlarmData));
    }

    private bool TryGettingFirstPipeNode(EntityUid uid, TransformComponent xform, MapGridComponent mapGrid, [NotNullWhen(true)] out PipeNode? pipeNode, [NotNullWhen(true)] out int? netId)
    {
        pipeNode = null;
        netId = null;

        if (xform.GridUid == null)
            return false;

        var gridIndex = _sharedMapSystem.TileIndicesFor(xform.GridUid.Value, mapGrid, xform.Coordinates);

        foreach (var node in NodeHelpers.GetNodesInTile(_nodeQuery, mapGrid, gridIndex))
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

    private AtmosMonitoringConsoleEntry? CreateAtmosMonitoringConsoleEntry
        (EntityUid uid, AtmosMonitoringConsoleComponent component, MapGridComponent mapGrid, TransformComponent xform, EntityUid ent, TransformComponent entXform, out AtmosFocusDeviceData? focusAlarmData)
    {
        AtmosMonitoringConsoleEntry? entry = null;
        focusAlarmData = null;

        var netEnt = GetNetEntity(ent);
        var name = MetaData(ent).EntityName;

        if (entXform.GridUid == null)
            return null;

        if (TryComp<LabelComponent>(ent, out var label) && label.CurrentLabel != null)
            name = label.CurrentLabel;

        if (TryComp<ApcPowerReceiverComponent>(ent, out var apcPowerReceiver) && !apcPowerReceiver.Powered)
        {
            entry = new AtmosMonitoringConsoleEntry
                (netEnt, GetNetCoordinates(entXform.Coordinates), AtmosMonitoringConsoleGroup.GasPipeSensor, name, "None")
            {
                IsActive = false
            };

            return entry;
        }

        if (!TryGettingFirstPipeNode(ent, entXform, mapGrid, out var pipeNode, out var netId))
            return entry;

        if (pipeNode != null && netId != null)
        {
            bool isAirPresent = pipeNode.Air.TotalMoles > 0;

            entry = new AtmosMonitoringConsoleEntry
                (netEnt, GetNetCoordinates(entXform.Coordinates), AtmosMonitoringConsoleGroup.GasPipeSensor, name, netId.Value.ToString())
            {
                TemperatureData = isAirPresent ? pipeNode.Air.Temperature : 0f,
                PressureData = pipeNode.Air.Pressure,
                TotalMolData = pipeNode.Air.TotalMoles
            };

            if (component.FocusDevice == ent)
            {
                var gasMixture = new Dictionary<Gas, float>();

                if (isAirPresent)
                {
                    foreach (var gas in Enum.GetValues<Gas>())
                    {
                        if (pipeNode.Air[(int)gas] > 0)
                            gasMixture.Add(gas, pipeNode.Air[(int)gas] / pipeNode.Air.TotalMoles);
                    }
                }

                focusAlarmData = new AtmosFocusDeviceData(netEnt, gasMixture, netId.Value);
            }
        }

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

        int netId = -1;

        if (TryComp<NodeContainerComponent>(uid, out var nodeContainer))
        {
            foreach ((var id, var node) in nodeContainer.Nodes)
            {
                if (node is not PipeNode)
                    continue;

                var pipeNode = (PipeNode)node;

                if (pipeNode.NodeGroup is BaseNodeGroup)
                {
                    var nodeGroup = (BaseNodeGroup)pipeNode.NodeGroup;
                    netId = nodeGroup.NetId;
                }
            }
        }

        device = new AtmosDeviceNavMapData(GetNetEntity(uid), GetNetCoordinates(xform.Coordinates), component.Group, netId, color, direction);

        return true;
    }

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
            var mask = (ulong)SharedNavMapSystem.AllDirMask << (tileIdx * SharedNavMapSystem.Directions);
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
            atmosPipeData |= (ulong)pipeDirection << (tileIdx * SharedNavMapSystem.Directions);
            chunk.AtmosPipeData[(netId, pipeColor.Color.ToHex())] = atmosPipeData;
        }
    }

    private void InitalizeAtmosMonitoringConsole(EntityUid uid, AtmosMonitoringConsoleComponent component)
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
}
