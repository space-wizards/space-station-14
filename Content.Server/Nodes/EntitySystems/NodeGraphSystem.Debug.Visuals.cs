using Content.Server.Nodes.Components;
using Content.Server.Nodes.Events.Debug;
using Content.Shared.Nodes;
using Robust.Server.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>The set of players that are receiving node debug info and a filter for which graphs they are receiving that info for.</summary>
    private readonly Dictionary<ICommonSession, GraphFilter> _visSessions = new();
    /// <summary>Whether we are currently sending node debug info to players.</summary>
    private bool _sendingVisState = false;



    private void UpdateDebugVisuals(EntityUid graphId, bool enabled, bool raiseEv = true, NodeGraphComponent? graph = null)
    {
        if (!_graphQuery.Resolve(graphId, ref graph))
            return;

        graph.NetSyncEnabled = _sendingVisState;
        Dirty(graphId, graph);

        // Fake the deletion of the node graph so the visuals go away.
        // Required since the client-side components stick around after we lose contact until _something_ is deleted.
        // Thus + not sending a new component state make the client think it's been deleted.
        if (!enabled)
        {
            AddComp<DebugNodeVisFakeDeletionPromptComponent>(graphId);
            RemComp<DebugNodeVisFakeDeletionPromptComponent>(graphId);
        }

        foreach (var nodeId in graph.Nodes)
        {
            if (!_graphQuery.TryGetComponent(nodeId, out var node))
                continue;

            node.NetSyncEnabled = _sendingVisState;
            Dirty(nodeId, node);

            // Ditto for the nodes.
            if (!enabled)
            {
                AddComp<DebugNodeVisFakeDeletionPromptComponent>(graphId);
                RemComp<DebugNodeVisFakeDeletionPromptComponent>(graphId);
            }
        }

        if (raiseEv)
        {
            // Raise events in case anyone else would like to get in on the action.
            var ev = new NodeVisViewersChanged(_sendingVisState);
            RaiseLocalEvent(ref ev);
        }
    }

    private void UpdateDebugVisuals(IEnumerable<EntityUid> graphs, bool enabled)
    {
        foreach (var graphId in graphs)
        {
            UpdateDebugVisuals(graphId, enabled, raiseEv: false);
        }

        // Raise events in case anyone else would like to get in on the action.
        var ev = new NodeVisViewersChanged(_sendingVisState);
        RaiseLocalEvent(ref ev);
    }

    private void UpdateDebugVisuals(bool enabled)
    {
        _sendingVisState = _visSessions.Count > 0;

        var graphQuery = EntityQueryEnumerator<NodeGraphComponent>();
        while (graphQuery.MoveNext(out var graphId, out var graph))
        {
            UpdateDebugVisuals(graphId, enabled, raiseEv: false, graph: graph);
        }

        // Raise events in case anyone else would like to get in on the action.
        var ev = new NodeVisViewersChanged(_sendingVisState);
        RaiseLocalEvent(ref ev);
    }


    #region Command Hooks

    /// <summary>Starts sending the debugging visuals for node graphs to a player.</summary>
    public void StartSendingNodeVis(ICommonSession session)
    {
        GraphFilter? filter = _visSessions.TryGetValue(session, out var tmp) ? tmp : null;

        _visSessions[session] = new GraphFilter()
        {
            Default = true,
            Uids = filter?.Uids,
            Prototypes = filter?.Prototypes,
        };

        UpdateDebugVisuals(enabled: true);
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: true), session);
    }

    /// <summary>Starts sending the visual state of a specific graphs to a player.</summary>
    public void StartSendingNodeVis(ICommonSession session, EntityUid graphId)
    {
        GraphFilter? filter = _visSessions.TryGetValue(session, out var tmp) ? tmp : null;

        if (filter?.Uids is { } uids)
            uids[graphId] = true;
        else
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = filter?.Default ?? false,
                Uids = new() { { graphId, true } },
                Prototypes = filter?.Prototypes,
            };
        }

        UpdateDebugVisuals(graphId, enabled: true);
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: true), session);
    }

    /// <summary>Stops sending the visual state of a specific types of node graph to a player.</summary>
    public void StartSendingNodeVis(ICommonSession session, string prototype)
    {
        GraphFilter? filter = _visSessions.TryGetValue(session, out var tmp) ? tmp : null;

        if (filter?.Prototypes is { } prototypes)
            prototypes[prototype] = true;
        else
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = filter?.Default ?? false,
                Uids = filter?.Uids,
                Prototypes = new() { { prototype, true } },
            };
        }

        UpdateDebugVisuals(GetGraphsByType(prototype), enabled: true);
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: true), session);
    }

    /// <summary>Stops sending the visual state of nodes to a player.</summary>
    public void StopSendingNodeVis(ICommonSession session)
    {
        _visSessions.Remove(session);

        UpdateDebugVisuals(enabled: false);
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: false), session);
    }

    /// <summary>Stops sending the visual state of a specific graph to a player.</summary>
    public void StopSendingNodeVis(ICommonSession session, EntityUid graphId)
    {
        if (!_visSessions.TryGetValue(session, out var filter))
            return;

        if (filter.Uids is { } uids)
            uids[graphId] = false;
        else
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = filter.Default,
                Uids = new() { { graphId, false } },
                Prototypes = filter.Prototypes,
            };
        }

        UpdateDebugVisuals(graphId, enabled: false);
    }

    /// <summary>Stops sending the visual state of a specific type of node graph to a player.</summary>
    public void StopSendingNodeVis(ICommonSession session, string prototype)
    {
        if (!_visSessions.TryGetValue(session, out var filter))
            return;

        if (filter.Prototypes is { } prototypes)
            prototypes[prototype] = false;
        else
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = filter.Default,
                Uids = filter.Uids,
                Prototypes = new() { { prototype, false } },
            };
        }

        UpdateDebugVisuals(GetGraphsByType(prototype), enabled: false);
    }

    #endregion Command Hooks


    #region Event Handlers

    /// <summary>Ensures that players visualizing node graphs receive the state of those node graphs since the majority of them are otherwise outside of PVS.</summary>
    private void OnExpandPvs(ref ExpandPvsEvent args)
    {
        if (!_sendingVisState)
            return;
        if (!_visSessions.TryGetValue(args.Session, out var filter))
            return;

        var graphs = EntityQueryEnumerator<NodeGraphComponent>();
        while (graphs.MoveNext(out var uid, out var graph))
        {
            if (filter.Uids?.TryGetValue(uid, out var shouldAdd) == true)
            {
                if (shouldAdd)
                {
                    args.Entities ??= new();
                    args.Entities.Add(uid);
                }
                continue;
            }

            if (filter.Prototypes?.TryGetValue(graph.GraphProto, out shouldAdd) == true)
            {
                if (shouldAdd)
                {
                    args.Entities ??= new();
                    args.Entities.Add(uid);
                }
                continue;
            }

            if (filter.Default)
            {
                args.Entities ??= new();
                args.Entities.Add(uid);
            }
        }
    }

    /// <summary>Ensures that only players visualizing node graphs can see their state.</summary>
    private void OnGetComponentStateAttempt(EntityUid uid, NodeGraphComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        if (!_sendingVisState)
            return;
        if (args.Player is not ICommonSession session)
            return;

        if (!_visSessions.TryGetValue(session, out var filter))
        {
            args.Cancelled = true;
            return;
        }

        if (filter.Uids?.TryGetValue(uid, out var shouldSend) == true)
        {
            args.Cancelled = !shouldSend;
            return;
        }

        if (filter.Prototypes?.TryGetValue(comp.GraphProto, out shouldSend) == true)
        {
            args.Cancelled = !shouldSend;
            return;
        }

        args.Cancelled = !filter.Default;
    }

    /// <summary>Ensures that only players visualizing node graphs can see their state.</summary>
    private void OnGetComponentStateAttempt(EntityUid uid, GraphNodeComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        if (!_sendingVisState)
            return;
        if (args.Player is not ICommonSession session)
            return;

        if (!_visSessions.TryGetValue(session, out var filter))
        {
            args.Cancelled = true;
            return;
        }

        if (filter.Uids?.TryGetValue(comp.GraphId ?? EntityUid.Invalid, out var shouldSend) == true)
        {
            args.Cancelled = !shouldSend;
            return;
        }

        if (filter.Prototypes?.TryGetValue(comp.GraphProto, out shouldSend) == true)
        {
            args.Cancelled = !shouldSend;
            return;
        }

        args.Cancelled = !filter.Default;
    }

    /// <summary>Ensures that only players visualizing node graphs can see their state.</summary>
    private void OnGetComponentState(EntityUid uid, NodeGraphComponent comp, ref ComponentGetState args)
    {
        args.State = new GraphVisState(
            color: comp.DebugColor,
            size: comp.Nodes.Count
        );
    }

    /// <summary>Ensures that only players visualizing node graphs can see their state.</summary>
    private void OnGetComponentState(EntityUid uid, GraphNodeComponent comp, ref ComponentGetState args)
    {
        var edges = new List<EdgeVisState>();
        foreach (var (edgeId, edgeFlags) in comp.Edges)
        {
            edges.Add(new(GetNetEntity(edgeId), edgeFlags));
        }

        args.State = new NodeVisState(
            edges: edges,
            hostId: GetNetEntity(GetNodeHost(uid, comp)),
            graphId: GetNetEntity(comp.GraphId),
            graphProto: comp.GraphProto
        );
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            StopSendingNodeVis(e.Session);
    }

    #endregion Event Handlers


    /// <summary>
    /// Determines which graphs get sent to an associated session.
    /// </summary>
    private readonly struct GraphFilter
    {
        /// <summary>
        /// Whether node graphs not specified below are rendered by default.
        /// </summary>
        public bool Default { get; init; }
        /// <summary>
        /// The set of specific graph ids that are or are not being sent to this session.
        /// Overrides the prototype filter for these specific graphs.
        /// </summary>
        public Dictionary<EntityUid, bool>? Uids { get; init; }

        /// <summary>
        /// The set of specific graph prototypes that are being sent to this session.
        /// If null assumed to be all types of graphs.
        /// </summary>
        public Dictionary<string, bool>? Prototypes { get; init; }
    }


    /// <summary>
    /// Component used to fake the 'deletion' of the debug node visuals for node graphs and graph nodes.
    /// </summary>
    [RegisterComponent]
    private sealed partial class DebugNodeVisFakeDeletionPromptComponent : Component
    {
    };
}
