using System.Collections.Frozen;
using System.Diagnostics;
using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.NodeContainer.Systems;

public sealed partial class NodeGroupSystem : EntitySystem
{
    [Dependency] private IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;
    [Dependency] private ISharedAdminManager _adminManager = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityQuery<NodeContainerComponent> _nodeContainerQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    public Dictionary<NodeGroupID, Type> NodeGroupTypes = new();
    private FrozenDictionary<NodeGroupID, Type> _nodeGroupTypes = default!;

    /// <summary>
    /// A dictionary of <see cref="INodeGroup"/> Types and <see cref="INodeGroupHandler"/>s.
    /// </summary>
    public Dictionary<Type, INodeGroupHandler> NodeGroupHandlers = new();
    private FrozenDictionary<Type, INodeGroupHandler> _nodeGroupHandlers = default!;

    /// <summary>
    /// A dictionary of <see cref="INode"/> Types and <see cref="INodeHandler"/>s.
    /// </summary>
    public Dictionary<Type, INodeHandler> NodeHandlers = new();
    private FrozenDictionary<Type, INodeHandler> _nodeHandlers = default!;

    // TODO figure what the hell is going on here
    private readonly List<int> _visDeletes = new();
    private readonly List<BaseNodeGroup> _visSends = new();

    private readonly HashSet<ICommonSession> _visPlayers = new();
    private readonly HashSet<BaseNodeGroup> _toRemake = new();
    private readonly HashSet<BaseNodeGroup> _nodeGroups = new();
    private readonly HashSet<Node> _toRemove = new();
    private readonly List<Node> _toReflood = new();

    private ISawmill _sawmill = default!;

    private const float VisDataUpdateInterval = 1;
    private float _accumulatedFrameTime;

    public bool VisEnabled => _visPlayers.Count != 0;

    private int _gen = 1;
    private int _groupNetIdCounter = 1;

    /// <summary>
    ///     If true, UpdateGrid() will not process grids.
    /// </summary>
    /// <remarks>
    ///     Useful if something like a large explosion is in the process of shredding the grid, as it avoids uneccesary
    ///     updating.
    /// </remarks>
    public bool PauseUpdating = false;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("nodegroup");

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeNetworkEvent<NodeVis.MsgEnable>(HandleEnableMsg);
    }

    public void PostInitialize()
    {
        _nodeGroupTypes = NodeGroupTypes.ToFrozenDictionary();
        _nodeGroupHandlers = NodeGroupHandlers.ToFrozenDictionary();
        _nodeHandlers = NodeHandlers.ToFrozenDictionary();
    }

    public INodeHandler GetNodeHandler(Type nodeType)
    {
        return _nodeHandlers[nodeType];
    }

    public INodeHandler GetNodeHandler(Node node)
    {
        return GetNodeHandler(node.GetType());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void HandleEnableMsg(NodeVis.MsgEnable msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Debug))
            return;

        if (msg.Enabled)
        {
            _visPlayers.Add(session);
            VisSendFullStateImmediate(session);
        }
        else
        {
            _visPlayers.Remove(session);
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            _visPlayers.Remove(e.Session);
    }

    public void QueueRemakeGroup(BaseNodeGroup group)
    {
        if (group.Remaking)
            return;

        _toRemake.Add(group);
        group.Remaking = true;

        foreach (var node in group.Nodes)
        {
            QueueReflood(node);
        }

        if (group.NodeCount == 0)
        {
            _nodeGroups.Remove(group);
        }
    }

    public void QueueReflood(Node node)
    {
        if (node.FlaggedForFlood)
            return;

        _toReflood.Add(node);
        node.FlaggedForFlood = true;
    }

    public void QueueNodeRemove(Node node)
    {
        _toRemove.Add(node);
    }

    public void CreateSingleNetImmediate(Node node)
    {
        if (node.NodeGroup != null)
            return;

        QueueReflood(node);

        InitGroup(node, new List<Node> {node});
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // TODO implement logic to split node groups into predicted/unpredicted ones
        if (_net.IsClient || PauseUpdating)
            return;

        DoGroupUpdates();
        VisDoUpdate(frameTime);
    }

    // used to manually force an update for the groups
    // the VisDoUpdate will be done with the next scheduled update
    public void ForceUpdate()
    {
        DoGroupUpdates();
    }

    private void DoGroupUpdates()
    {
        // "Why is there a separate queue for group remakes and node refloods when they both cause eachother"
        // Future planning for the potential ability to do more intelligent group updating.

        if (_toRemake.Count == 0 && _toReflood.Count == 0 && _toRemove.Count == 0)
            return;

        var sw = Stopwatch.StartNew();

        foreach (var toRemove in _toRemove)
        {
            if (toRemove.NodeGroup == null)
                continue;

            var group = (BaseNodeGroup) toRemove.NodeGroup;
            var groupHandler = _nodeGroupHandlers[group.GetType()];
            groupHandler.RemoveNode(group, toRemove);
            toRemove.NodeGroup = null;

            QueueRemakeGroup(group);
        }

        // Break up all remaking groups.
        // Don't clear the list yet, we'll come back to these later.
        foreach (var toRemake in _toRemake)
        {
            QueueRemakeGroup(toRemake);
        }

        _gen += 1;

        // Go over all nodes to calculate reachable nodes and make an undirected graph out of them.
        // Node.GetReachableNodes() may return results asymmetrically,
        // i.e. node A may return B, but B may not return A.
        //
        // Must be for loop to allow concurrent modification from RemakeGroupImmediate.
        for (var i = 0; i < _toReflood.Count; i++)
        {
            var node = _toReflood[i];

            if (node.Deleting)
                continue;

            ClearReachableIfNecessary(node);

            if (node.NodeGroup?.Remaking == false)
            {
                QueueRemakeGroup((BaseNodeGroup) node.NodeGroup);
            }

            // GetCompatibleNodes will involve getting the transform & grid as most connection requirements are
            // based on position & anchored neighbours However, here more than one node could be attached to the
            // same parent. So there is probably a better way of doing this.

            foreach (var compatible in GetCompatibleNodes(node))
            {
                ClearReachableIfNecessary((Node) compatible);

                if (compatible.NodeGroup?.Remaking == false)
                {
                    // We are expanding into an existing group,
                    // remake it so that we can treat it uniformly.
                    var group = (BaseNodeGroup) compatible.NodeGroup;
                    QueueRemakeGroup(group);
                }

                node.ReachableNodes.Add((Node) compatible);
                compatible.ReachableNodes.Add(node);
            }
        }

        var newGroups = new List<BaseNodeGroup>();

        // Flood fill over nodes. Every node will only be flood filled once.
        foreach (var node in _toReflood)
        {
            node.FlaggedForFlood = false;

            // Check if already flood filled.
            if (node.FloodGen == _gen || node.Deleting)
                continue;

            // Flood fill
            var groupNodes = FloodFillNode(node);

            var newGroup = InitGroup(node, groupNodes);
            newGroups.Add(newGroup);
        }

        // Go over dead groups that need to be cleaned up.
        // Tell them to push their data to new groups too.
        foreach (var oldGroup in _toRemake)
        {
            // Group by the NEW group.
            var newGrouped = oldGroup.Nodes.GroupBy(n => n.NodeGroup);

            oldGroup.Removed = true;
            var handler = _nodeGroupHandlers[oldGroup.GetType()];
            handler.AfterRemake(oldGroup, newGrouped);
            _nodeGroups.Remove(oldGroup);
            if (VisEnabled)
                _visDeletes.Add(oldGroup.NetId);
        }

        var refloodCount = _toReflood.Count;

        _toReflood.Clear();
        _toRemake.Clear();
        _toRemove.Clear();

        // notify entities that node groups have been updated, so they can do things like update their visuals.
        HashSet<EntityUid> entities = new();
        foreach (var group in newGroups)
        {
            foreach (var node in group.Nodes)
            {
                entities.Add(node.Owner);
            }
        }

        foreach (var uid in entities)
        {
            var ev = new NodeGroupsRebuilt(uid);
            RaiseLocalEvent(uid, ref ev, true);
        }

        _sawmill.Debug($"Updated node groups in {sw.Elapsed.TotalMilliseconds}ms. {newGroups.Count} new groups, {refloodCount} nodes processed.");
    }

    private void ClearReachableIfNecessary(Node node)
    {
        if (node.UndirectGen != _gen)
        {
            node.ReachableNodes.Clear();
            node.UndirectGen = _gen;
        }
    }

    private BaseNodeGroup InitGroup(Node node, List<Node> groupNodes)
    {
        var type = _nodeGroupTypes[node.NodeGroupID];
        var handler = _nodeGroupHandlers[type];
        var newGroup = (BaseNodeGroup) _typeFactory.CreateInstance(type);
        handler.InitializeGroup(newGroup, node);
        newGroup.NetId = _groupNetIdCounter++;

        var netIdCounter = 0;
        foreach (var groupNode in groupNodes)
        {
            groupNode.NodeGroup = newGroup;
            groupNode.NetId = ++netIdCounter;
        }

        handler.LoadNodes(newGroup, groupNodes);

        _nodeGroups.Add(newGroup);

        if (VisEnabled)
            _visSends.Add(newGroup);

        return newGroup;
    }

    private List<Node> FloodFillNode(Node rootNode)
    {
        // All nodes we're filling into that currently have NO network.
        var allNodes = new List<Node>();

        var stack = new Stack<Node>();
        stack.Push(rootNode);
        rootNode.FloodGen = _gen;

        while (stack.TryPop(out var node))
        {
            allNodes.Add(node);

            foreach (var reachable in node.ReachableNodes)
            {
                if (reachable.FloodGen == _gen)
                    continue;

                reachable.FloodGen = _gen;
                stack.Push(reachable);
            }
        }

        return allNodes;
    }

    private IEnumerable<INode> GetCompatibleNodes(Node node)
    {
        var xform = Transform(node.Owner);
        Entity<MapGridComponent>? gridEnt = TryComp<MapGridComponent>(xform.GridUid, out var grid) ? (xform.GridUid.Value, grid) : null;

        var nodeHandler = _nodeHandlers[node.GetType()];
        if (!nodeHandler.Connectable(node))
            yield break;

        foreach (var reachable in nodeHandler.GetReachableNodes(node))
        {
            DebugTools.Assert(reachable != node, "GetReachableNodes() should not include self.");

            var reachableNodeHandler = _nodeHandlers[reachable.GetType()];
            if (reachable.NodeGroupID == node.NodeGroupID
                && reachableNodeHandler.Connectable(reachable))
            {
                yield return reachable;
            }
        }
    }

    private void VisDoUpdate(float frametime)
    {
        if (_visPlayers.Count == 0)
            return;

        _accumulatedFrameTime += frametime;

        if (_accumulatedFrameTime < VisDataUpdateInterval
            && _visSends.Count == 0
            && _visDeletes.Count == 0)
            return;

        var msg = new NodeVis.MsgData();

        msg.GroupDeletions.AddRange(_visDeletes);
        msg.Groups.AddRange(_visSends.Select(VisMakeGroupState));

        if (_accumulatedFrameTime > VisDataUpdateInterval)
        {
            _accumulatedFrameTime -= VisDataUpdateInterval;
            foreach (var group in _nodeGroups)
            {
                if (_visSends.Contains(group))
                    continue;

                var handler = _nodeGroupHandlers[group.GetType()];
                msg.GroupDataUpdates.Add(group.NetId, handler.GetDebugData(group));
            }
        }

        _visSends.Clear();
        _visDeletes.Clear();

        foreach (var player in _visPlayers)
        {
            RaiseNetworkEvent(msg, player.Channel);
        }
    }

    private void VisSendFullStateImmediate(ICommonSession player)
    {
        var msg = new NodeVis.MsgData();

        foreach (var network in _nodeGroups)
        {
            msg.Groups.Add(VisMakeGroupState(network));
        }

        RaiseNetworkEvent(msg, player.Channel);
    }

    private NodeVis.GroupData VisMakeGroupState(BaseNodeGroup group)
    {
        var handler = _nodeGroupHandlers[group.GetType()];
        return new()
        {
            NetId = group.NetId,
            GroupId = group.GroupId.ToString(),
            Color = CalcNodeGroupColor(group),
            Nodes = group.Nodes.Select(n => new NodeVis.NodeDatum
            {
                Name = n.Name,
                NetId = n.NetId,
                Reachable = n.ReachableNodes.Select(r => r.NetId).ToArray(),
                Entity = GetNetEntity(n.Owner),
                Type = n.GetType().Name
            }).ToArray(),
            DebugData = handler.GetDebugData(group),
        };
    }

    private static Color CalcNodeGroupColor(BaseNodeGroup group)
    {
        return group.GroupId switch
        {
            NodeGroupID.HVPower => Color.Orange,
            NodeGroupID.MVPower => Color.Yellow,
            NodeGroupID.Apc => Color.LimeGreen,
            NodeGroupID.AMEngine => Color.Purple,
            NodeGroupID.Pipe => Color.Blue,
            NodeGroupID.WireNet => Color.DarkMagenta,
            NodeGroupID.Teg => Color.Red,
            NodeGroupID.ExCable => Color.Pink,
            _ => Color.White
        };
    }
}

/// <summary>
///     Event raised after node groups have been updated. Directed at any entity with a <see
///     cref="NodeContainerComponent"/> that had a relevant node.
/// </summary>
[ByRefEvent]
public record struct NodeGroupsRebuilt(EntityUid NodeOwner);
