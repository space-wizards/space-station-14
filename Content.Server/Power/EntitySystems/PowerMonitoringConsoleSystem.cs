using Content.Shared.Power;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Shared.Construction.Components;
using System.Numerics;
using Robust.Shared.Map;
using Content.Server.NodeContainer.NodeGroups;
using System.Threading.Tasks;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class PowerMonitoringConsoleSystem : EntitySystem
{
    private float _updateTimer = 0.0f;
    private const float UpdateTime = 1.0f;

    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = EntityQueryEnumerator<PowerMonitoringConsoleComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                UpdateUIState(uid, component);
            }
        }
    }

    public void UpdateUIState(EntityUid target, PowerMonitoringConsoleComponent? pmcComp = null, NodeContainerComponent? ncComp = null)
    {
        if (!Resolve(target, ref pmcComp))
            return;

        if (!Resolve(target, ref ncComp))
            return;

        if (!_userInterfaceSystem.TryGetUi(target, PowerMonitoringConsoleUiKey.Key, out var bui))
            return;

        var consoleXform = _entityManager.GetComponent<TransformComponent>(target);
        if (consoleXform?.GridUid == null)
            return;

        var sources = new List<PowerMonitoringConsoleEntry>();
        var loads = new List<PowerMonitoringConsoleEntry>();

        //PowerConsumerComponent
        //BatteryChargerComponent

        //PowerSupplierComponent
        //BatteryDischargerComponent

        var query = AllEntityQuery<PowerNetworkBatteryComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var networkBattery, out var xform))
        {
            if (xform.Anchored)
            {
                var metaData = MetaData(networkBattery.Owner);
                var prototype = metaData.EntityPrototype?.ID ?? "";

                loads.Add(new PowerMonitoringConsoleEntry(_entityManager.GetNetEntity(uid), GetNetCoordinates(xform.Coordinates), metaData.EntityName, prototype, networkBattery.NetworkBattery.CurrentReceiving, true));
            }
        }

        GetPowerCableCoordinates(consoleXform.GridUid.Value, out var hvCableCoords, out var mvCableCoords, out var lvCableCoords);

        // Sort
        loads.Sort(CompareLoadOrSources);
        sources.Sort(CompareLoadOrSources);

        // Actually set state.
        if (_userInterfaceSystem.TryGetUi(target, PowerMonitoringConsoleUiKey.Key, out bui))
           _userInterfaceSystem.SetUiState(bui, new PowerMonitoringConsoleBoundInterfaceState(loads.ToArray(), hvCableCoords, mvCableCoords, lvCableCoords, true, 10f));
    }

    private void GetPowerCableCoordinates(EntityUid gridUid, out NetCoordinates[][] hvCableCoords, out NetCoordinates[][] mvCableCoords, out NetCoordinates[][] lvCableCoords)
    {
        var _hvCableCoords = new List<NetCoordinates[]>();
        var _mvCableCoords = new List<NetCoordinates[]>();
        var _lvCableCoords = new List<NetCoordinates[]>();

        var query = AllEntityQuery<NodeContainerComponent, TransformComponent>();
 
        while (query.MoveNext(out var uid, out var nodeContainer, out var xform))
        {
            if (!_nodeContainer.TryGetNode<Node>(nodeContainer, "power", out var node))
                continue;

            if (node.NodeGroupID != NodeGroupID.HVPower && node.NodeGroupID != NodeGroupID.MVPower && node.NodeGroupID != NodeGroupID.Apc)
                continue;

            if (!xform.Anchored)
                continue;

            if (xform.GridUid != gridUid)
                continue;

            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
                continue;

            foreach (var adjacent in grid.GetCardinalNeighborCells(xform.Coordinates))
            {
                if (!TryComp<TransformComponent>(adjacent, out var adjXform))
                    continue;

                if (!TryComp<NodeContainerComponent>(adjacent, out var adjNodeContainer))
                    continue;

                if (!_nodeContainer.TryGetNode<Node>(adjNodeContainer, "power", out var adjNode))
                    continue;

                if (adjNode.NodeGroupID != node.NodeGroupID)
                    continue;

                var coords = new NetCoordinates[] { GetNetCoordinates(xform.Coordinates), GetNetCoordinates(adjXform.Coordinates) };

                switch (node.NodeGroupID)
                {
                    case NodeGroupID.HVPower:
                        _hvCableCoords.Add(coords); break;
                    case NodeGroupID.MVPower:
                        _mvCableCoords.Add(coords); break;
                    case NodeGroupID.Apc:
                        _lvCableCoords.Add(coords); break;
                }
            }
        }

        hvCableCoords = _hvCableCoords.ToArray();
        mvCableCoords = _mvCableCoords.ToArray();
        lvCableCoords = _lvCableCoords.ToArray();
    }

    private int CompareLoadOrSources(PowerMonitoringConsoleEntry x, PowerMonitoringConsoleEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }
}
