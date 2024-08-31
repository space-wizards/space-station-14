using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

//using static Content.Shared.Power.SharedPowerMonitoringConsoleSystem;

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

    public override void Initialize()
    {
        base.Initialize();

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

        if (!HasComp<MapGridComponent>(gridUid))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        EnsureComp<NavMapComponent>(gridUid);

        // Gathering remaining data to be send to the client
        AtmosFocusDeviceData? focusAlarmData = null;

        var pumps = new AtmosMonitoringConsoleEntry[0];
        var mixers = new AtmosMonitoringConsoleEntry[0];
        var filters = new AtmosMonitoringConsoleEntry[0];
        var thermoregulators = new AtmosMonitoringConsoleEntry[0];

        // Set the UI state
        _userInterfaceSystem.SetUiState(uid, AtmosMonitoringConsoleUiKey.Key,
            new AtmosMonitoringConsoleBoundInterfaceState(pumps, mixers, filters, thermoregulators, focusAlarmData));
    }



    private AtmosFocusDeviceData? GetFocusAlarmData(EntityUid uid, AtmosMonitoringConsoleComponent component, EntityUid gridUid)
    {
        if (component.FocusDevice == null)
            return null;

        var ent = component.FocusDevice.Value;
        var entXform = Transform(component.FocusDevice.Value);

        if (!entXform.Anchored ||
            entXform.GridUid != gridUid ||
            !TryComp<AirAlarmComponent>(ent, out var entAirAlarm))
        {
            return null;
        }

        //return new AtmosFocusDeviceData(GetNetEntity(component.FocusDevice.Value), temperatureData, pressureData, gasData);
        return null;
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

        device = new AtmosDeviceNavMapData(GetNetEntity(uid), GetNetCoordinates(xform.Coordinates), component.Group, color, direction);

        return true;
    }

    public static int ChunkSize = 4;

    public static int GetFlag(Vector2i relativeTile)
    {
        return 1 << (relativeTile.X * ChunkSize + relativeTile.Y);
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

            if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
                chunk = new AtmosPipeChunk(chunkOrigin);

            if (!chunk.AtmosPipeData.TryGetValue(entAtmosPipeColor.Color.ToHex(), out var atmosPipeData))
                atmosPipeData = new AtmosPipeData();

            var relative = SharedMapSystem.GetChunkRelative(tile.GridIndices, ChunkSize);

            foreach ((var id, var node) in entNodeContainer.Nodes)
            {
                if (node is not PipeNode)
                    continue;

                var pipeNode = node as PipeNode;
                var pipeDirection = pipeNode!.CurrentPipeDirection;

                var flagNorth = (((int)pipeDirection & (int)PipeDirection.North) > 0) ? (ushort)GetFlag(relative) : (ushort)0;
                var flagSouth = (((int)pipeDirection & (int)PipeDirection.South) > 0) ? (ushort)GetFlag(relative) : (ushort)0;
                var flagEast = (((int)pipeDirection & (int)PipeDirection.East) > 0) ? (ushort)GetFlag(relative) : (ushort)0;
                var flagWest = (((int)pipeDirection & (int)PipeDirection.West) > 0) ? (ushort)GetFlag(relative) : (ushort)0;

                atmosPipeData.NorthFacing |= flagNorth;
                atmosPipeData.SouthFacing |= flagSouth;
                atmosPipeData.EastFacing |= flagEast;
                atmosPipeData.WestFacing |= flagWest;

                chunk.AtmosPipeData[entAtmosPipeColor.Color.ToHex()] = atmosPipeData;
            }

            allChunks[chunkOrigin] = chunk;
        }

        return allChunks;
    }

    private void RebuildSingleTileOfPipeNetwork(EntityUid gridUid, MapGridComponent grid, EntityCoordinates coordinates)
    {
        if (!_gridAtmosPipeChunks.TryGetValue(gridUid, out var allChunks))
            allChunks = new Dictionary<Vector2i, AtmosPipeChunk>();

        var tile = _sharedMapSystem.GetTileRef(gridUid, grid, coordinates);
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, ChunkSize);
        var relative = SharedMapSystem.GetChunkRelative(tile.GridIndices, ChunkSize);

        if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
            chunk = new AtmosPipeChunk(chunkOrigin);

        foreach (var ent in _sharedMapSystem.GetAnchoredEntities(gridUid, grid, coordinates))
        {
            if (!TryComp<NodeContainerComponent>(ent, out var entNodeContainer))
                continue;

            if (!TryComp<AtmosPipeColorComponent>(ent, out var entAtmosPipeColor))
                continue;

            if (!chunk.AtmosPipeData.TryGetValue(entAtmosPipeColor.Color.ToHex(), out var atmosPipeData))
                atmosPipeData = new AtmosPipeData();

            chunk.AtmosPipeData[entAtmosPipeColor.Color.ToHex()] = UpdateAtmosPipeData(atmosPipeData, relative, entNodeContainer);
        }

        allChunks[chunkOrigin] = chunk;

        var query = AllEntityQuery<AtmosMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (gridUid != entXform.GridUid)
                continue;

            entConsole.AtmosPipeChunks = allChunks;
            Dirty(ent, entConsole);
        }
    }

    private AtmosPipeData UpdateAtmosPipeData(AtmosPipeData atmosPipeData, Vector2i positionInChunk, NodeContainerComponent nodeContainer)
    {
        foreach ((var _, var node) in nodeContainer.Nodes)
        {
            if (node is not PipeNode)
                continue;

            var pipeNode = node as PipeNode;
            var pipeDirection = pipeNode!.CurrentPipeDirection;

            var flagNorth = (((int)pipeDirection & (int)PipeDirection.North) > 0) ? (ushort)GetFlag(positionInChunk) : (ushort)0;
            var flagSouth = (((int)pipeDirection & (int)PipeDirection.South) > 0) ? (ushort)GetFlag(positionInChunk) : (ushort)0;
            var flagEast = (((int)pipeDirection & (int)PipeDirection.East) > 0) ? (ushort)GetFlag(positionInChunk) : (ushort)0;
            var flagWest = (((int)pipeDirection & (int)PipeDirection.West) > 0) ? (ushort)GetFlag(positionInChunk) : (ushort)0;

            atmosPipeData.NorthFacing |= flagNorth;
            atmosPipeData.SouthFacing |= flagSouth;
            atmosPipeData.EastFacing |= flagEast;
            atmosPipeData.WestFacing |= flagWest;
        }

        return atmosPipeData;
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
