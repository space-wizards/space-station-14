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
using Content.Shared.Power;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class PowerMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerMonitoringConsoleComponent, RequestPowerMonitoringDataMessage>(OnDataRequestReceived);
    }

    private void OnDataRequestReceived(EntityUid uid, PowerMonitoringConsoleComponent component, RequestPowerMonitoringDataMessage args)
    {
        UpdateUIState(uid, component);
    }

    public void UpdateUIState(EntityUid target, PowerMonitoringConsoleComponent? powerMonitoring = null)
    {
        if (!_userInterfaceSystem.TryGetUi(target, PowerMonitoringConsoleUiKey.Key, out var bui))
            return;

        if (!Resolve(target, ref powerMonitoring))
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
                    powerSupplier.CurrentSupply,
                    true);

                sources.Add(entry);
                totalSources += powerSupplier.CurrentSupply;
            }
        }

        // Network batteries
        var networkBatteryQuery = AllEntityQuery<PowerNetworkBatteryComponent, MetaDataComponent, TransformComponent>();
        while (networkBatteryQuery.MoveNext(out var uid, out var networkBattery, out var metaData, out var xform))
        {
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
                totalLoads += networkBattery.NetworkBattery.CurrentReceiving;
            }
        }

        // Sort
        //loads.Sort(CompareLoadOrSources);
        //sources.Sort(CompareLoadOrSources);

        // Get power cable data
        var powerCableChunks = GetPowerCableChunks(mapGrid);

        // Actually set state
        _userInterfaceSystem.SetUiState(bui, new PowerMonitoringConsoleBoundInterfaceState(loads.ToArray(), powerCableChunks, true, 10f));
    }

    private Dictionary<Vector2i, NavMapChunkPowerCables> GetPowerCableChunks(MapGridComponent grid)
    {
        var chunks = new Dictionary<Vector2i, NavMapChunkPowerCables>();
        var tiles = grid.GetAllTilesEnumerator();

        while (tiles.MoveNext(out var tile))
        {
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.Value.GridIndices, SharedNavMapSystem.ChunkSize);

            if (!chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new NavMapChunkPowerCables(chunkOrigin);
                chunks[chunkOrigin] = chunk;
            }

            var gridIndices = tile.Value.GridIndices;
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
        }

        return chunks;
    }

    private int CompareLoadOrSources(PowerMonitoringConsoleEntry x, PowerMonitoringConsoleEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }
}
