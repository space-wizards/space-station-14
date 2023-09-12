using Content.Server.Nodes.Components;
using Content.Server.Nodes.Events.Debug;
using Content.Shared.Nodes;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using System.Linq;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>The set of players that are receiving node debug info and a filter for which graphs they are receiving that info for.</summary>
    private readonly Dictionary<IPlayerSession, GraphFilter> _visSessions = new();
    /// <summary>Whether we are currently sending node debug info to players.</summary>
    private bool _sendingVisState = false;


    private void UpdateDebugVisuals()
    {
        var shouldSendVisState = _visSessions.Count > 0;
        _sendingVisState = shouldSendVisState;

        var graphs = EntityQueryEnumerator<NodeGraphComponent>();
        while (graphs.MoveNext(out var graphId, out var graph))
        {
            graph.NetSyncEnabled = shouldSendVisState;

            if (shouldSendVisState)
                Dirty(graphId, graph);
        }

        var nodes = EntityQueryEnumerator<GraphNodeComponent>();
        while (nodes.MoveNext(out var nodeId, out var node))
        {
            node.NetSyncEnabled = shouldSendVisState;

            if (shouldSendVisState)
                Dirty(nodeId, node);
        }

        // Raise events in case anyone else would like to get in on the action.
        var ev = new NodeVisViewersChanged(shouldSendVisState);
        RaiseLocalEvent(ref ev);
    }


    #region Command Hooks

    /// <summary>Starts sending the visual state of nodes to a player.</summary>
    public void StartSendingNodeVis(IPlayerSession session)
    {
        if (_visSessions.TryGetValue(session, out var filter))
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = true,
                Uids = filter.Uids,
                Prototypes = filter.Prototypes,
            };
        }
        else
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = true,
                Uids = null,
                Prototypes = null,
            };
        }

        UpdateDebugVisuals();
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: true), session);
    }

    /// <summary>Starts sending the visual state of specific graphs to a player.</summary>
    public void StartSendingNodeVis(IPlayerSession session, List<EntityUid> uids)
    {
        if (uids.Count <= 0)
            return;


        if (_visSessions.TryGetValue(session, out var filter))
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = false,
                Uids = new(uids.Select(uid => KeyValuePair.Create(uid, true))),
                Prototypes = null,
            };
        }
        else if (filter.Uids is null)
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = filter.Default,
                Uids = new(uids.Select(uid => KeyValuePair.Create(uid, true))),
                Prototypes = filter.Prototypes,
            };
        }
        else
        {
            foreach (var uid in uids)
            {
                filter.Uids[uid] = true;
            }
        }

        UpdateDebugVisuals();
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: true), session);
    }

    /// <summary>Stops sending the visual state of specific types of graphs to a player.</summary>
    public void StartSendingNodeVis(IPlayerSession session, List<string> prototypes)
    {
        if (prototypes.Count <= 0)
            return;


        if (_visSessions.TryGetValue(session, out var filter))
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = false,
                Uids = null,
                Prototypes = new(prototypes.Select(prototype => KeyValuePair.Create(prototype, true))),
            };
        }
        else if (filter.Prototypes is null)
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = filter.Default,
                Uids = filter.Uids,
                Prototypes = new(prototypes.Select(prototype => KeyValuePair.Create(prototype, true))),
            };
        }
        else
        {
            foreach (var prototype in prototypes)
            {
                filter.Prototypes[prototype] = true;
            }
        }

        UpdateDebugVisuals();
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: true), session);
    }

    /// <summary>Stops sending the visual state of nodes to a player.</summary>
    public void StopSendingNodeVis(IPlayerSession session)
    {
        _visSessions.Remove(session);

        UpdateDebugVisuals();
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: false), session);
    }

    /// <summary>Stops sending the visual state of specific graphs to a player.</summary>
    public void StopSendingNodeVis(IPlayerSession session, List<EntityUid> uids)
    {
        if (uids.Count <= 0)
            return;

        if (!_visSessions.TryGetValue(session, out var filter))
            return;

        if (filter.Uids is null)
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = filter.Default,
                Uids = new(uids.Select(uid => KeyValuePair.Create(uid, false))),
                Prototypes = filter.Prototypes,
            };
        }
        else
        {
            foreach (var uid in uids)
            {
                filter.Uids[uid] = false;
            }
        }


        UpdateDebugVisuals();
    }

    /// <summary>Stops sending the visual state of specific types of graphs to a player.</summary>
    public void StopSendingNodeVis(IPlayerSession session, List<string> prototypes)
    {
        if (prototypes.Count <= 0)
            return;

        if (!_visSessions.TryGetValue(session, out var filter))
            return;

        if (filter.Prototypes is null)
        {
            _visSessions[session] = new GraphFilter()
            {
                Default = filter.Default,
                Uids = filter.Uids,
                Prototypes = new(prototypes.Select(prototype => KeyValuePair.Create(prototype, false))),
            };
        }
        else
        {
            foreach (var prototype in prototypes)
            {
                filter.Prototypes[prototype] = false;
            }
        }

        UpdateDebugVisuals();
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
                    args.Entities.Add(uid);
                continue;
            }

            if (filter.Prototypes?.TryGetValue(graph.GraphProto, out shouldAdd) == true)
            {
                if (shouldAdd)
                    args.Entities.Add(uid);
                continue;
            }

            if (filter.Default)
                args.Entities.Add(uid);
        }
    }

    /// <summary>Ensures that only players visualizing node graphs can see their state.</summary>
    private void OnGetComponentStateAttempt(EntityUid uid, NodeGraphComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        if (!_sendingVisState)
            return;
        if (args.Player is not IPlayerSession session)
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
        if (args.Player is not IPlayerSession session)
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
            edges.Add(new(edgeId, edgeFlags));
        }

        args.State = new NodeVisState(
            edges: edges,
            hostId: GetNodeHost(uid, comp),
            graphId: comp.GraphId,
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
}
