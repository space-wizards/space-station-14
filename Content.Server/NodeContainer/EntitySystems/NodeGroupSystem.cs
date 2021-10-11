using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Administration;
using Content.Shared.NodeContainer;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.NodeContainer.EntitySystems
{
    /// <summary>
    ///     Entity system that manages <see cref="NodeGroupSystem"/> and <see cref="Node"/> updating.
    /// </summary>
    /// <seealso cref="NodeContainerSystem"/>
    [UsedImplicitly]
    public class NodeGroupSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly INodeGroupFactory _nodeGroupFactory = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private readonly List<int> _visDeletes = new();
        private readonly List<BaseNodeGroup> _visSends = new();

        private readonly HashSet<IPlayerSession> _visPlayers = new();
        private readonly HashSet<BaseNodeGroup> _toRemake = new();
        private readonly HashSet<Node> _toRemove = new();
        private readonly List<Node> _toReflood = new();

        private ISawmill _sawmill = default!;

        public bool VisEnabled => _visPlayers.Count != 0;

        private int _gen = 1;
        private int _groupNetIdCounter = 1;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = _logManager.GetSawmill("nodegroup");

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            SubscribeNetworkEvent<NodeVis.MsgEnable>(HandleEnableMsg);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private void HandleEnableMsg(NodeVis.MsgEnable msg, EntitySessionEventArgs args)
        {
            var session = (IPlayerSession) args.SenderSession;
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

            DoGroupUpdates();
            VisDoUpdate();
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

                group.RemoveNode(toRemove);
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

                foreach (var compatible in GetCompatibleNodes(node))
                {
                    ClearReachableIfNecessary(compatible);

                    if (compatible.NodeGroup?.Remaking == false)
                    {
                        // We are expanding into an existing group,
                        // remake it so that we can treat it uniformly.
                        var group = (BaseNodeGroup) compatible.NodeGroup;
                        QueueRemakeGroup(group);
                    }

                    node.ReachableNodes.Add(compatible);
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
                oldGroup.AfterRemake(newGrouped);
                if (VisEnabled)
                    _visDeletes.Add(oldGroup.NetId);
            }

            var refloodCount = _toReflood.Count;

            _toReflood.Clear();
            _toRemake.Clear();
            _toRemove.Clear();

            foreach (var group in newGroups)
            {
                foreach (var node in group.Nodes)
                {
                    node.OnPostRebuild();
                }
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
            var newGroup = (BaseNodeGroup) _nodeGroupFactory.MakeNodeGroup(node.NodeGroupID);
            newGroup.Initialize(node);
            newGroup.NetId = _groupNetIdCounter++;

            var netIdCounter = 0;
            foreach (var groupNode in groupNodes)
            {
                groupNode.NodeGroup = newGroup;
                groupNode.NetId = ++netIdCounter;
            }

            newGroup.LoadNodes(groupNodes);

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

        private static IEnumerable<Node> GetCompatibleNodes(Node node)
        {
            foreach (var reachable in node.GetReachableNodes())
            {
                DebugTools.Assert(reachable != node, "GetReachableNodes() should not include self.");

                if (reachable.Connectable && reachable.NodeGroupID == node.NodeGroupID)
                    yield return reachable;
            }
        }

        private void VisDoUpdate()
        {
            if (_visSends.Count == 0 && _visDeletes.Count == 0)
                return;

            var msg = new NodeVis.MsgData();

            msg.GroupDeletions.AddRange(_visDeletes);
            msg.Groups.AddRange(_visSends.Select(VisMakeGroupState));

            _visSends.Clear();
            _visDeletes.Clear();

            foreach (var player in _visPlayers)
            {
                RaiseNetworkEvent(msg, player.ConnectedClient);
            }
        }

        private void VisSendFullStateImmediate(IPlayerSession player)
        {
            var msg = new NodeVis.MsgData();

            var allNetworks = EntityManager
                .EntityQuery<NodeContainerComponent>()
                .SelectMany(nc => nc.Nodes.Values)
                .Select(n => (BaseNodeGroup?) n.NodeGroup)
                .Where(n => n != null)
                .Distinct();

            foreach (var network in allNetworks)
            {
                msg.Groups.Add(VisMakeGroupState(network!));
            }

            RaiseNetworkEvent(msg, player.ConnectedClient);
        }

        private static NodeVis.GroupData VisMakeGroupState(BaseNodeGroup group)
        {
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
                    Entity = n.Owner.Uid,
                    Type = n.GetType().Name
                }).ToArray()
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
                _ => Color.White
            };
        }
    }
}
