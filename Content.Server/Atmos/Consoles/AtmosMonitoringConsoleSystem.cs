using Content.Server.Atmos.Components;
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
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Atmos.Consoles;

public sealed class AtmosMonitoringConsoleSystem : SharedAtmosMonitoringConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

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
        SubscribeLocalEvent<AtmosMonitoringConsoleDeviceComponent, NodeGroupsRebuilt>(OnEntityNodeGroupsRebuilt);
        SubscribeLocalEvent<AtmosMonitoringConsoleDeviceComponent, AtmosPipeColorChangedEvent>(OnEntityPipeColorChanged);
        SubscribeLocalEvent<AtmosMonitoringConsoleDeviceComponent, EntityTerminatingEvent>(OnEntityShutdown);

        // Grid events
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
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
        component.ForceFullUpdate = true;
        InitializeAtmosMonitoringConsole(uid, component);
    }

    private void OnEntityNodeGroupsRebuilt(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component, NodeGroupsRebuilt args)
    {
        InitializeAtmosMonitoringDevice(uid, component);
    }

    private void OnEntityPipeColorChanged(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component, AtmosPipeColorChangedEvent args)
    {
        InitializeAtmosMonitoringDevice(uid, component);
    }

    private void OnEntityShutdown(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component, EntityTerminatingEvent args)
    {
        ShutDownAtmosMonitoringEntity(uid, component);
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

        // Gathering data to be send to the client
        var atmosNetworks = new List<AtmosMonitoringConsoleEntry>();
        var query = AllEntityQuery<GasPipeSensorComponent, TransformComponent>();

        while (query.MoveNext(out var ent, out var entSensor, out var entXform))
        {
            if (entXform?.GridUid != xform.GridUid)
                continue;

            if (!entXform.Anchored)
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

        var pipeColor = TryComp<AtmosPipeColorComponent>(uid, out var colorComponent) ? colorComponent.Color : Color.White;

        // Name the entity based on its label, if available
        if (TryComp<LabelComponent>(uid, out var label) && label.CurrentLabel != null)
            name = label.CurrentLabel;

        // Otherwise use its base name and network address
        else if (TryComp<DeviceNetworkComponent>(uid, out var deviceNet))
            address = deviceNet.Address;

        // Entry for unpowered devices
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver) && !apcPowerReceiver.Powered)
        {
            entry = new AtmosMonitoringConsoleEntry(netEnt, GetNetCoordinates(xform.Coordinates), netId.Value, name, address)
            {
                IsPowered = false,
                Color = pipeColor
            };

            return entry;
        }

        // Entry for powered devices
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

        entry = new AtmosMonitoringConsoleEntry(netEnt, GetNetCoordinates(xform.Coordinates), netId.Value, name, address)
        {
            TemperatureData = isAirPresent ? pipeNode.Air.Temperature : 0f,
            PressureData = pipeNode.Air.Pressure,
            TotalMolData = pipeNode.Air.TotalMoles,
            GasData = gasData,
            Color = pipeColor
        };

        return entry;
    }

    private Dictionary<NetEntity, AtmosDeviceNavMapData> GetAllAtmosDeviceNavMapData(EntityUid gridUid)
    {
        var atmosDeviceNavMapData = new Dictionary<NetEntity, AtmosDeviceNavMapData>();

        var query = AllEntityQuery<AtmosMonitoringConsoleDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entComponent, out var entXform))
        {
            if (TryGetAtmosDeviceNavMapData(ent, entComponent, entXform, gridUid, out var data))
                atmosDeviceNavMapData.Add(data.Value.NetEntity, data.Value);
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

        if (component.NavMapBlip == null)
            return false;

        if (xform.GridUid != gridUid)
            return false;

        if (!xform.Anchored)
            return false;

        var direction = xform.LocalRotation.GetCardinalDir();

        if (!TryGettingFirstPipeNode(uid, out var _, out var netId))
            netId = -1;

        var color = Color.White;

        if (TryComp<AtmosPipeColorComponent>(uid, out var atmosPipeColor))
            color = atmosPipeColor.Color;

        device = new AtmosDeviceNavMapData(GetNetEntity(uid), GetNetCoordinates(xform.Coordinates), netId.Value, component.NavMapBlip.Value, direction, color);

        return true;
    }

    #endregion

    #region Pipe net functions

    private void RebuildAtmosPipeGrid(EntityUid gridUid, MapGridComponent grid)
    {
        var allChunks = new Dictionary<Vector2i, AtmosPipeChunk>();

        // Adds all atmos pipes to the nav map via bit mask chunks
        var queryPipes = AllEntityQuery<AtmosPipeColorComponent, NodeContainerComponent, TransformComponent>();
        while (queryPipes.MoveNext(out var ent, out var entAtmosPipeColor, out var entNodeContainer, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            if (!entXform.Anchored)
                continue;

            var tile = _sharedMapSystem.GetTileRef(gridUid, grid, entXform.Coordinates);
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, ChunkSize);
            var relative = SharedMapSystem.GetChunkRelative(tile.GridIndices, ChunkSize);

            if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new AtmosPipeChunk(chunkOrigin);
                allChunks[chunkOrigin] = chunk;
            }

            UpdateAtmosPipeChunk(ent, entNodeContainer, entAtmosPipeColor, GetTileIndex(relative), ref chunk);
        }

        // Add or update the chunks on the associated grid
        _gridAtmosPipeChunks[gridUid] = allChunks;

        // Update the consoles that are on the same grid
        var queryConsoles = AllEntityQuery<AtmosMonitoringConsoleComponent, TransformComponent>();
        while (queryConsoles.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (gridUid != entXform.GridUid)
                continue;

            entConsole.AtmosPipeChunks = allChunks;
            Dirty(ent, entConsole);
        }
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

        // Rebuild the tile's pipe data 
        foreach (var ent in _sharedMapSystem.GetAnchoredEntities(gridUid, grid, coords))
        {
            if (!TryComp<AtmosPipeColorComponent>(ent, out var entAtmosPipeColor))
                continue;

            if (!TryComp<NodeContainerComponent>(ent, out var entNodeContainer))
                continue;

            UpdateAtmosPipeChunk(ent, entNodeContainer, entAtmosPipeColor, tileIdx, ref chunk);
        }

        // Add or update the chunk on the associated grid
        // Only the modified chunk will be sent to the client
        chunk.LastUpdate = _gameTiming.CurTick;
        allChunks[chunkOrigin] = chunk;
        _gridAtmosPipeChunks[gridUid] = allChunks;

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
        // Entities that are actively being deleted are not to be drawn
        if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

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

        component.AtmosDevices = GetAllAtmosDeviceNavMapData(grid);

        if (!_gridAtmosPipeChunks.TryGetValue(grid, out var chunks))
        {
            RebuildAtmosPipeGrid(grid, map);
        }

        else
        {
            component.AtmosPipeChunks = chunks;
            Dirty(uid, component);
        }
    }

    private void InitializeAtmosMonitoringDevice(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component)
    {
        // Rebuild tile
        var xform = Transform(uid);
        var gridUid = xform.GridUid;

        if (gridUid != null && TryComp<MapGridComponent>(gridUid, out var grid))
            RebuildSingleTileOfPipeNetwork(gridUid.Value, grid, xform.Coordinates);

        // Update blips on affected consoles
        if (component.NavMapBlip == null)
            return;

        var netEntity = EntityManager.GetNetEntity(uid);
        var query = AllEntityQuery<AtmosMonitoringConsoleComponent, TransformComponent>();

        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            var isDirty = entConsole.AtmosDevices.Remove(netEntity);

            if (gridUid != null &&
                gridUid == entXform.GridUid &&
                xform.Anchored &&
                TryGetAtmosDeviceNavMapData(uid, component, xform, gridUid.Value, out var data))
            {
                entConsole.AtmosDevices.Add(netEntity, data.Value);
                isDirty = true;
            }

            if (isDirty)
                Dirty(ent, entConsole);
        }
    }

    private void ShutDownAtmosMonitoringEntity(EntityUid uid, AtmosMonitoringConsoleDeviceComponent component)
    {
        // Rebuild tile
        var xform = Transform(uid);
        var gridUid = xform.GridUid;

        if (gridUid != null && TryComp<MapGridComponent>(gridUid, out var grid))
            RebuildSingleTileOfPipeNetwork(gridUid.Value, grid, xform.Coordinates);

        // Update blips on affected consoles
        if (component.NavMapBlip == null)
            return;

        var netEntity = EntityManager.GetNetEntity(uid);
        var query = AllEntityQuery<AtmosMonitoringConsoleComponent>();

        while (query.MoveNext(out var ent, out var entConsole))
        {
            if (entConsole.AtmosDevices.Remove(netEntity))
                Dirty(ent, entConsole);
        }
    }

    #endregion

    private int GetTileIndex(Vector2i relativeTile)
    {
        return relativeTile.X * ChunkSize + relativeTile.Y;
    }
}
