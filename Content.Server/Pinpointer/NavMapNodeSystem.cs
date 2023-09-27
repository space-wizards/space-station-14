using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Station.Systems;
using Content.Shared.Pinpointer;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Server.Pinpointer;

public sealed class NavMapNodeSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<StationGridAddedEvent>(OnStationInit);
    }

    private void OnStationInit(StationGridAddedEvent ev)
    {
        var gridUid = ev.GridId;
        var comp = EnsureComp<NavMapNodeComponent>(gridUid);
        var nodes = new List<EntityUid>();

        var query = AllEntityQuery<NodeContainerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var nodeContainer, out var xform))
        {
            if (xform.GridUid != gridUid || !xform.Anchored)
                continue;

            if (!nodeContainer.Nodes.Values.Any
                (x => x.NodeGroupID == NodeGroupID.HVPower ||
                x.NodeGroupID == NodeGroupID.MVPower ||
                x.NodeGroupID == NodeGroupID.Apc))
                continue;

            nodes.Add(uid);
        }

        comp.QueuedNodesToAdd = nodes;
        //AddQueuedNodes(gridUid, comp);
    }

    /*public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = EntityQueryEnumerator<NavMapNodeComponent>();
            while (query.MoveNext(out var gridUid, out var component))
            {
                RemoveQueuedNodes(gridUid, component);
                AddQueuedNodes(gridUid, component);
            }
        }
    }*/
    private void AddQueuedNodes(EntityUid gridUid, NavMapNodeComponent navMapNode)
    {
        var hvNodes = new List<NetCoordinates>();
        var mvNodes = new List<NetCoordinates>();
        var lvNodes = new List<NetCoordinates>();

        foreach (EntityUid uid in navMapNode.QueuedNodesToAdd)
        {
            if (_entityManager.Deleted(uid))
                continue;

            if (!TryComp<NodeContainerComponent>(uid, out var nodeContainer) ||
                !_nodeContainer.TryGetNode<Node>(nodeContainer, "power", out var node))
                continue;

            if (!TryComp<TransformComponent>(uid, out var xform) ||
                xform.GridUid != gridUid ||
                !xform.Anchored)
                continue;

            var netCoords = GetNetCoordinates(xform.Coordinates);

            switch (node.NodeGroupID)
            {
                case NodeGroupID.HVPower:
                    hvNodes.Add(netCoords); break;
                case NodeGroupID.MVPower:
                    lvNodes.Add(netCoords); break;
                case NodeGroupID.Apc:
                    mvNodes.Add(netCoords); break;
            }
        }

        //RaiseNetworkEvent(new NavMapAddNodesMessage(GetNetEntity(gridUid), hvNodes, mvNodes, lvNodes));
    }

    private void RemoveQueuedNodes(EntityUid gridUid, NavMapNodeComponent navMapNode)
    {
        var hvNodes = new List<NetCoordinates>();
        var mvNodes = new List<NetCoordinates>();
        var lvNodes = new List<NetCoordinates>();

        foreach ((var nodeGroupID, var coordsList) in navMapNode.QueuedNodesToRemove)
        {
            foreach (var coords in coordsList)
            {
                var netCoords = GetNetCoordinates(coords);

                switch (nodeGroupID)
                {
                    case NodeGroupID.HVPower:
                        hvNodes.Add(netCoords); break;
                    case NodeGroupID.MVPower:
                        lvNodes.Add(netCoords); break;
                    case NodeGroupID.Apc:
                        mvNodes.Add(netCoords); break;
                }
            }
        }

        //RaiseNetworkEvent(new NavMapRemoveNodesMessage(GetNetEntity(gridUid), hvNodes, mvNodes, lvNodes));
    }
}
