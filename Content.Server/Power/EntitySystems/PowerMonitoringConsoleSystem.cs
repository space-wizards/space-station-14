using Content.Shared.Power;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Server.Pinpointer;
using Content.Shared.Pinpointer;
using Robust.Shared.Map.Components;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.EntitySystems;
using Robust.Server.GameStates;
using Robust.Shared.Players;
using Content.Server.Power.NodeGroups;
using System.Linq;
using Robust.Shared.Map;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class PowerMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverrideSystem = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerMonitoringConsoleComponent, RequestPowerMonitoringDataMessage>(OnDataRequestReceived);
    }

    private void OnDataRequestReceived(EntityUid uid, PowerMonitoringConsoleComponent component, RequestPowerMonitoringDataMessage args)
    {
        UpdateUIState(uid, component, args.NetEntity, args.Session);
    }

    public void UpdateUIState(EntityUid target, PowerMonitoringConsoleComponent powerMonitoring, NetEntity? netEntity, ICommonSession session)
    {
        if (!_userInterfaceSystem.TryGetUi(target, PowerMonitoringConsoleUiKey.Key, out var bui))
            return;

        var consoleXform = _entityManager.GetComponent<TransformComponent>(target);
        if (consoleXform?.GridUid == null)
            return;

        MapGridComponent? mapGrid = null;
        if (!Resolve(consoleXform.GridUid.Value, ref mapGrid))
            return;

        var totalSources = 0f;
        var totalLoads = 0f;
        var sources = new List<PowerMonitoringConsoleEntry>();
        var loads = new List<PowerMonitoringConsoleEntry>();

        //Power suppliers
        var powerSupplierQuery = AllEntityQuery<PowerSupplierComponent, MetaDataComponent, TransformComponent>();
        while (powerSupplierQuery.MoveNext(out var uid, out var powerSupplier, out var metaData, out var xform))
        {
            if (xform.Anchored)
            {
                var prototype = metaData.EntityPrototype?.ID ?? "";
                var entry = new PowerMonitoringConsoleEntry
                    (_entityManager.GetNetEntity(uid),
                    GetNetCoordinates(xform.Coordinates),
                    metaData.EntityName,
                    prototype,
                    powerSupplier.MaxSupply,
                    true);

                sources.Add(entry);
                _pvsOverrideSystem.AddSessionOverride(uid, session, true);

                totalSources += powerSupplier.MaxSupply;
            }
        }

        // Network batteries
        var networkBatteryQuery = AllEntityQuery<PowerNetworkBatteryComponent, MetaDataComponent, TransformComponent>();
        while (networkBatteryQuery.MoveNext(out var uid, out var networkBattery, out var metaData, out var xform))
        {
            if (_entityManager.HasComponent<PowerSupplierComponent>(uid))
                continue;

            if (xform.Anchored)
            {
                var prototype = metaData.EntityPrototype?.ID ?? "";

                var entry = new PowerMonitoringConsoleEntry
                    (_entityManager.GetNetEntity(uid),
                    GetNetCoordinates(xform.Coordinates),
                    metaData.EntityName,
                    prototype,
                    networkBattery.NetworkBattery.CurrentReceiving,
                    true);

                loads.Add(entry);
                _pvsOverrideSystem.AddSessionOverride(uid, session, true);

                totalLoads += networkBattery.NetworkBattery.CurrentReceiving;
            }
        }

        var _sources = new List<PowerMonitoringConsoleEntry>();
        var _loads = new List<PowerMonitoringConsoleEntry>();
        var _output = new Dictionary<Vector2i, NavMapChunkPowerCables>();

        if (netEntity != null)
        {
            var uid = _entityManager.GetEntity(netEntity);

            if (uid != null &&
                _entityManager.TryGetComponent<NodeContainerComponent>(uid, out var nodeContainer) &&
                _entityManager.TryGetComponent<PowerMonitoringDeviceComponent>(uid, out var powerMonitoringDevice))
            {
                Logger.Debug("Get data for: " + uid);
                List<Node> reachableSources = new List<Node>();
                List<Node> reachableLoads = new List<Node>();

                var xform = Transform(uid.Value);
                if (nodeContainer.Nodes.TryGetValue(powerMonitoringDevice.SourceNode, out var sourceNode))
                {
                    GetTotalSourcesForNode(uid.Value, sourceNode, out _sources);
                    reachableSources = FloodFillNode(sourceNode);
                }

                if (nodeContainer.Nodes.TryGetValue(powerMonitoringDevice.LoadNode, out var loadNode))
                {
                    GetTotalLoadsForNode(uid.Value, loadNode, out _loads);
                    reachableLoads = FloodFillNode(loadNode);
                }

                var reachableNodes = reachableSources.Concat(reachableLoads).ToList();

                /*var gridIndices = mapGrid.TileIndicesFor(xform.Coordinates);
                var enumerator = mapGrid.GetAnchoredEntitiesEnumerator(gridIndices);

                while (enumerator.MoveNext(out var ent))
                {
                    if (!TryComp<NodeContainerComponent>(ent, out var nodeContainerNew) ||
                        !_nodeContainer.TryGetNode<Node>(nodeContainerNew, "power", out var node))
                        continue;

                    if (node.NodeGroupID != sourceNode?.NodeGroupID && node.NodeGroupID != loadNode?.NodeGroupID)
                        continue;

                    //reachableNodes = node.ReachableNodes.ToList();
                    
                    Logger.Debug("reachables: " + reachableNodes.Count());

                    
                }*/

                _output = GetSpecificPowerCables(mapGrid, reachableNodes);
                Logger.Debug("reachables: " + reachableNodes.Count);
                Logger.Debug("no. sources: " + _sources.Count);
                Logger.Debug("no. loads: " + _loads.Count);
            }
        }

        // Sort
        //loads.Sort(CompareLoadOrSources);
        //sources.Sort(CompareLoadOrSources);

        // Get power cable data
        var powerCableChunks = GetPowerCableChunks(mapGrid);

        // Actually set state
        _userInterfaceSystem.SetUiState(bui,
            new PowerMonitoringConsoleBoundInterfaceState
                (totalSources,
                totalLoads,
                sources.ToArray(),
                loads.ToArray(),
                _sources.ToArray(),
                _loads.ToArray(),
                powerCableChunks,
                _output));
    }

    private double GetTotalSourcesForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> sources)
    {
        var totalSources = 0.0d;
        sources = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return totalSources;

        foreach (PowerSupplierComponent powerSupplier in netQ.Suppliers)
        {
            if (uid == powerSupplier.Owner)
                continue;

            var supply = powerSupplier.Enabled ? powerSupplier.MaxSupply : 0f;

            sources.Add(LoadOrSource(powerSupplier, supply, false));
            totalSources += supply;
        }

        foreach (var batteryDischarger in netQ.Dischargers)
        {
            if (uid == batteryDischarger.Owner)
                continue;

            if (!TryComp(batteryDischarger.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            var rate = batteryComp.NetworkBattery.CurrentSupply;
            sources.Add(LoadOrSource(batteryDischarger, rate, true));
            totalSources += rate;
        }

        return totalSources;
    }

    private double GetTotalLoadsForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> loads)
    {
        var totalLoads = 0.0d;
        loads = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return totalLoads;

        foreach (PowerConsumerComponent powerConsumer in netQ.Consumers)
        {
            if (uid == powerConsumer.Owner)
                continue;

            if (!powerConsumer.ShowInMonitor)
                continue;

            loads.Add(LoadOrSource(powerConsumer, powerConsumer.DrawRate, false));
            totalLoads += powerConsumer.DrawRate;
        }

        foreach (var batteryCharger in netQ.Chargers)
        {
            if (uid == batteryCharger.Owner)
                continue;

            if (!TryComp(batteryCharger.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            var rate = batteryComp.NetworkBattery.CurrentReceiving;
            loads.Add(LoadOrSource(batteryCharger, rate, true));
            totalLoads += rate;
        }

        return totalLoads;
    }

    private PowerMonitoringConsoleEntry LoadOrSource(Component component, double rate, bool isBattery)
    {
        var metaData = MetaData(component.Owner);
        var xform = Transform(component.Owner);
        var coordinates = _entityManager.GetNetCoordinates(xform.Coordinates);
        var netEntity = _entityManager.GetNetEntity(component.Owner);
        var id = metaData.EntityPrototype != null ? metaData.EntityPrototype.ID : "none";

        return new PowerMonitoringConsoleEntry(netEntity, coordinates, metaData.EntityName, id, rate, isBattery);
    }

    private Dictionary<Vector2i, NavMapChunkPowerCables> GetPowerCableChunks(MapGridComponent grid)
    {
        var chunks = new Dictionary<Vector2i, NavMapChunkPowerCables>();
        var tiles = grid.GetAllTilesEnumerator();

        while (tiles.MoveNext(out var tile))
        {
            var gridIndices = tile.Value.GridIndices;
            var chunkOrigin = SharedMapSystem.GetChunkIndices(gridIndices, SharedNavMapSystem.ChunkSize);

            if (!chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new NavMapChunkPowerCables(chunkOrigin);
                chunks[chunkOrigin] = chunk;
            }

            var relative = SharedMapSystem.GetChunkRelative(gridIndices, SharedNavMapSystem.ChunkSize);
            var flag = SharedNavMapSystem.GetFlag(relative);

            chunk.CableData[CableType.HighVoltage] &= ~flag;
            chunk.CableData[CableType.MediumVoltage] &= ~flag;
            chunk.CableData[CableType.Apc] &= ~flag;

            var enumerator = grid.GetAnchoredEntitiesEnumerator(gridIndices);
            while (enumerator.MoveNext(out var ent))
            {
                if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer) ||
                    !_nodeContainer.TryGetNode<Node>(nodeContainer, "power", out var node))
                    continue;

                switch (node.NodeGroupID)
                {
                    case NodeGroupID.HVPower: chunk.CableData[CableType.HighVoltage] |= flag; break;
                    case NodeGroupID.MVPower: chunk.CableData[CableType.MediumVoltage] |= flag; break;
                    case NodeGroupID.Apc: chunk.CableData[CableType.Apc] |= flag; break;
                }
            }

            if (chunk.CableData[CableType.HighVoltage] == 0 &&
                chunk.CableData[CableType.MediumVoltage] == 0 &&
                chunk.CableData[CableType.Apc] == 0)
                chunks.Remove(chunkOrigin);
        }

        return chunks;
    }

    private Dictionary<Vector2i, NavMapChunkPowerCables> GetSpecificPowerCables(MapGridComponent grid, IEnumerable<Node> list)
    {
        var chunks = new Dictionary<Vector2i, NavMapChunkPowerCables>();

        foreach (var node in list)
        {
            if (node == null)
                continue;

            var xform = Transform(node.Owner);
            var tile = _sharedMapSystem.GetTileRef(grid.Owner, grid, xform.Coordinates);
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, SharedNavMapSystem.ChunkSize);

            if (!chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new NavMapChunkPowerCables(chunkOrigin);
                chunks[chunkOrigin] = chunk;
            }

            var gridIndices = tile.GridIndices;
            var relative = SharedMapSystem.GetChunkRelative(gridIndices, SharedNavMapSystem.ChunkSize);
            var flag = SharedNavMapSystem.GetFlag(relative);

            switch (node.NodeGroupID)
            {
                case NodeGroupID.HVPower: chunk.CableData[CableType.HighVoltage] |= flag; break;
                case NodeGroupID.MVPower: chunk.CableData[CableType.MediumVoltage] |= flag; break;
                case NodeGroupID.Apc: chunk.CableData[CableType.Apc] |= flag; break;
            }
        }

        return chunks;
    }

    private List<Node> FloodFillNode(Node rootNode)
    {
        rootNode.FloodGen += 1;
        var allNodes = new List<Node>();
        var stack = new Stack<Node>();
        stack.Push(rootNode);

        while (stack.TryPop(out var node))
        {
            allNodes.Add(node);

            foreach (var reachable in node.ReachableNodes)
            {
                if (reachable.FloodGen == rootNode.FloodGen)
                    continue;

                reachable.FloodGen = rootNode.FloodGen;
                stack.Push(reachable);
            }
        }

        return allNodes;
    }

    private int CompareLoadOrSources(PowerMonitoringConsoleEntry x, PowerMonitoringConsoleEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }
}
