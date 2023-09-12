using Content.Server.Nodes.Components;
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
    private readonly Dictionary<IPlayerSession, GraphFilter?> _visSessions = new();
    /// <summary>Whether we are currently sending node debug info to players.</summary>
    private bool _sendingVisState = false;


    private void UpdateDebugVisuals()
    {
        var shouldSendVisState = _visSessions.Count > 0;
        if (_sendingVisState == shouldSendVisState)
            return;


        _sendingVisState = shouldSendVisState;

        var graphs = EntityQueryEnumerator<NodeGraphComponent>();
        while (graphs.MoveNext(out var _, out var graph))
        {
            graph.NetSyncEnabled = shouldSendVisState;
        }

        var nodes = EntityQueryEnumerator<GraphNodeComponent>();
        while (nodes.MoveNext(out var _, out var node))
        {
            node.NetSyncEnabled = shouldSendVisState;
        }
    }


    #region Command Hooks

    /// <summary>Starts sending the visual state of nodes to a player.</summary>
    public void StartSendingNodeVis(IPlayerSession session)
    {
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: true), session);
        _visSessions[session] = null;
    }

    /// <summary>Stops sending the visual state of nodes to a player.</summary>
    public void StopSendingNodeVis(IPlayerSession session)
    {
        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: false), session);
        _visSessions.Remove(session);
    }

    /// <summary>Starts sending the visual state of specific graphs to a player.</summary>
    public void StartSendingNodeVis(IPlayerSession session, List<EntityUid> uids)
    {
        if (uids.Count <= 0)
            return;

        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: true), session);

        if (!_visSessions.TryGetValue(session, out var filterOrNull) || filterOrNull is not { } filter || filter.Uids is null)
        {
            _visSessions[session] = new GraphFilter()
            {
                Uids = new(uids.Select(uid => KeyValuePair.Create(uid, true))),
                Prototypes = filterOrNull?.Prototypes,
            };
            return;
        }

        foreach (var uid in uids)
        {
            filter.Uids[uid] = true;
        }
    }

    /// <summary>Stops sending the visual state of specific graphs to a player.</summary>
    public void StopSendingNodeVis(IPlayerSession session, List<EntityUid> uids)
    {
        if (uids.Count <= 0)
            return;

        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: false), session);

        if (!_visSessions.TryGetValue(session, out var filterOrNull) || filterOrNull is not { } filter || filter.Uids is null)
        {
            _visSessions[session] = new GraphFilter()
            {
                Uids = new(uids.Select(uid => KeyValuePair.Create(uid, false))),
                Prototypes = filterOrNull?.Prototypes,
            };
            return;
        }

        foreach (var uid in uids)
        {
            filter.Uids[uid] = false;
        }
    }

    /// <summary>Stops sending the visual state of specific types of graphs to a player.</summary>
    public void StartSendingNodeVis(IPlayerSession session, List<string> prototypes)
    {
        if (prototypes.Count <= 0)
            return;

        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: true), session);

        if (!_visSessions.TryGetValue(session, out var filterOrNull) || filterOrNull is not { } filter || filter.Prototypes is null)
        {
            _visSessions[session] = new GraphFilter()
            {
                Uids = filterOrNull?.Uids,
                Prototypes = new(prototypes),
            };
            return;
        }

        foreach (var prototype in prototypes)
        {
            filter.Prototypes.Add(prototype);
        }
    }

    /// <summary>Stops sending the visual state of specific types of graphs to a player.</summary>
    public void StopSendingNodeVis(IPlayerSession session, List<string> prototypes)
    {
        if (prototypes.Count <= 0)
            return;

        RaiseNetworkEvent(new EnableNodeVisMsg(enabled: false), session);

        if (!_visSessions.TryGetValue(session, out var filterOrNull) || filterOrNull is not { } filter || filter.Prototypes is null)
            return;

        foreach (var prototype in prototypes)
        {
            filter.Prototypes.Remove(prototype);
        }
    }

    #endregion Command Hooks


    #region Event Handlers

    /// <summary>Ensures that players visualizing node graphs receive the state of those node graphs since the majority of them are otherwise outside of PVS.</summary>
    private void OnExpandPvs(ref ExpandPvsEvent args)
    {
        if (!_sendingVisState)
            return;
        if (!_visSessions.TryGetValue(args.Session, out var filterOrNull))
            return;

        var graphs = EntityQueryEnumerator<NodeGraphComponent>();
        while (graphs.MoveNext(out var uid, out var graph))
        {
            if (filterOrNull is not { } filter)
            {
                args.Entities.Add(uid);
                continue;
            }

            if (filter.Uids?.TryGetValue(uid, out var shouldAdd) == true)
            {
                if (shouldAdd)
                    args.Entities.Add(uid);
                continue;
            }

            if (filter.Prototypes?.Contains(graph.GraphProto) != false)
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

        if (!_visSessions.TryGetValue(session, out var filterOrNull))
        {
            args.Cancelled = true;
            return;
        }

        if (filterOrNull is not { } filter)
            return;

        if (filter.Uids?.TryGetValue(uid, out var shouldSend) == true)
        {
            args.Cancelled = !shouldSend;
            return;
        }

        args.Cancelled = filter.Prototypes?.Contains(comp.GraphProto) == false;
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

        if (!_visSessions.TryGetValue(session, out var filterOrNull))
        {
            args.Cancelled = true;
            return;
        }

        if (filterOrNull is not { } filter)
            return;

        if (filter.Uids?.TryGetValue(comp.GraphId ?? EntityUid.Invalid, out var shouldSend) == true)
        {
            args.Cancelled = !shouldSend;
            return;
        }

        args.Cancelled = filter.Prototypes?.Contains(comp.GraphProto) == false;
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
        /// The set of specific graph ids that are or are not being sent to this session.
        /// Overrides the prototype filter for these specific graphs.
        /// </summary>
        public Dictionary<EntityUid, bool>? Uids { get; init; }

        /// <summary>
        /// The set of specific graph prototypes that are being sent to this session.
        /// If null assumed to be all types of graphs.
        /// </summary>
        public HashSet<string>? Prototypes { get; init; }
    }
}
