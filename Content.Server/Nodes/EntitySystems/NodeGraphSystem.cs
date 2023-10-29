using Content.Server.Nodes.Components;
using Content.Server.Nodes.Events;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly MapSystem _mapSys = default!;
    private EntityQuery<GraphNodeComponent> _nodeQuery = default!;
    private EntityQuery<NodeGraphComponent> _graphQuery = default!;
    private EntityQuery<PolyNodeComponent> _polyQuery = default!;
    private EntityQuery<ProxyNodeComponent> _proxyQuery = default!;
    private EntityQuery<TransformComponent> _xformQuery = default!;

    private readonly Dictionary<string, HashSet<EntityUid>> _graphsByProto = new();

    /// <summary></summary>
    private readonly HashSet<EntityUid> _queuedEdgeUpdates = new();
    /// <summary></summary>
    private readonly HashSet<EntityUid> _queuedMergeGraphs = new();
    /// <summary></summary>
    private readonly HashSet<EntityUid> _queuedSplitGraphs = new();


    public override void Initialize()
    {
        base.Initialize();

        _nodeQuery = GetEntityQuery<GraphNodeComponent>();
        _graphQuery = GetEntityQuery<NodeGraphComponent>();
        _polyQuery = GetEntityQuery<PolyNodeComponent>();
        _proxyQuery = GetEntityQuery<ProxyNodeComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();


        SubscribeLocalEvent<GraphNodeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PolyNodeComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<GraphNodeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PolyNodeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NodeGraphComponent, EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<GraphNodeComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<NodeGraphComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<PolyNodeComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ProxyNodeComponent, ComponentShutdown>(OnComponentShutdown);

        // Proxy/Poly node relays:
        SubscribeLocalEvent<PolyNodeComponent, AnchorStateChangedEvent>(RelayPolyNodeRefEvent);
        SubscribeLocalEvent<PolyNodeComponent, ReAnchorEvent>(RelayPolyNodeRefEvent);
        SubscribeLocalEvent<PolyNodeComponent, MoveEvent>(RelayPolyNodeRefEvent);
        SubscribeLocalEvent<PolyNodeComponent, ExaminedEvent>(RelayPolyNodeValEvent<ExaminedEvent>);
        SubscribeLocalEvent<ProxyNodeComponent, EdgeAddedEvent>(RelayProxyNodeRefEvent);
        SubscribeLocalEvent<ProxyNodeComponent, EdgeRemovedEvent>(RelayProxyNodeRefEvent);
        SubscribeLocalEvent<ProxyNodeComponent, EdgeChangedEvent>(RelayProxyNodeRefEvent);
        SubscribeLocalEvent<ProxyNodeComponent, AddedToGraphEvent>(RelayProxyNodeRefEvent);
        SubscribeLocalEvent<ProxyNodeComponent, RemovedFromGraphEvent>(RelayProxyNodeRefEvent);

        // Debug info dispatching:
        SubscribeLocalEvent<ExpandPvsEvent>(OnExpandPvs);
        SubscribeLocalEvent<NodeGraphComponent, ComponentGetStateAttemptEvent>(OnGetComponentStateAttempt);
        SubscribeLocalEvent<GraphNodeComponent, ComponentGetStateAttemptEvent>(OnGetComponentStateAttempt);
        SubscribeLocalEvent<NodeGraphComponent, ComponentGetState>(OnGetComponentState);
        SubscribeLocalEvent<GraphNodeComponent, ComponentGetState>(OnGetComponentState);

        _playerMan.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        _playerMan.PlayerStatusChanged -= OnPlayerStatusChanged;

        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        const int maxUpdateIters = 100;

        var curTick = _gameTiming.CurTick;
        var iter = 0;
        bool loop;
        do
        {
            if (iter >= maxUpdateIters)
            {
                Log.Error($"Failed to resolve node graphs within {maxUpdateIters} iterations.");
                break;
            }
            loop = false;

            var updateIter = new UpdateIter(curTick, iter);


            // Update the edges on any nodes that need it.
            if (_queuedEdgeUpdates.Count > 0)
            {
                while (_queuedEdgeUpdates.FirstOrNull() is { } nodeId)
                {
                    UpdateEdges((nodeId, _nodeQuery.GetComponent(nodeId)));
                }
            }

            // Check graphs that may have been split for whether we need to split them.
            if (_queuedSplitGraphs.Count > 0)
            {
                loop = true;

                while (_queuedSplitGraphs.FirstOrNull() is { } graphId)
                {
                    ResolveSplits((graphId, _graphQuery.GetComponent(graphId)), updateIter);
                }
            }

            // Check graphs that may have been merged for whether we need to merge them.
            if (_queuedMergeGraphs.Count > 0)
            {
                loop = true;

                while (_queuedMergeGraphs.FirstOrNull() is { } graphId)
                {
                    ResolveMerges((graphId, _graphQuery.GetComponent(graphId)), updateIter);
                }
            }

            ++iter;
        } while (loop);
    }
}
