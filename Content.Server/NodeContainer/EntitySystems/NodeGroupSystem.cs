using System.Collections.Generic;
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
using Robust.Shared.Utility;

namespace Content.Server.NodeContainer.EntitySystems
{
    [UsedImplicitly]
    public class NodeGroupSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly INodeGroupFactory _nodeGroupFactory = default!;

        private readonly Queue<NodeGroupDebugVisMsg> _visQueue = new();
        private readonly HashSet<IPlayerSession> _visPlayers = new();
        private readonly HashSet<BaseNodeGroup> _toRemake = new();
        private readonly HashSet<Node> _toRemove = new();
        private readonly List<Node> _toReflood = new();
        public bool VisEnabled => _visPlayers.Count != 0;

        private int _gen = 1;

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            SubscribeNetworkEvent<NodeVis.MsgEnable>(HandleEnableMsg);
        }

        private void HandleEnableMsg(NodeVis.MsgEnable msg, EntitySessionEventArgs args)
        {
            var session = (IPlayerSession) args.SenderSession;
            if (_adminManager.HasAdminFlag(session, AdminFlags.Debug))
                return;

            if (msg.Enabled)
            {
                _visPlayers.Add(session);
                SendFullStateImmediate(session);
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
        }

        private void DoGroupUpdates()
        {
            if (_toRemake.Count == 0 && _toReflood.Count == 0 && _toRemove.Count == 0)
                return;

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
            // Node.GetReachableNodes() may return results asymmetrically, namely that
            // Must be for loop to allow concurrent modification from RemakeGroupImmediate.
            for (var i = 0; i < _toReflood.Count; i++)
            {
                var node = _toReflood[i];

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

            // Flood fill over nodes. Every node will only be flood filled once.
            foreach (var node in _toReflood)
            {
                node.FlaggedForFlood = false;

                // Check if already flood filled.
                if (node.FloodGen == _gen || node.Deleting)
                    continue;

                // Flood fill
                var groupNodes = FloodFillNode(node);

                InitGroup(node, groupNodes);
            }

            // Go over dead groups that need to be cleaned up.
            // Tell them to push their data to new groups too.
            foreach (var oldGroup in _toRemake)
            {
                // Group by the NEW group.
                var newGrouped = oldGroup.Nodes.GroupBy(n => n.NodeGroup);

                oldGroup.Removed = true;
                oldGroup.AfterRemake(newGrouped);
            }

            _toReflood.Clear();
            _toRemake.Clear();
            _toRemove.Clear();
        }

        private void ClearReachableIfNecessary(Node node)
        {
            if (node.UndirectGen != _gen)
            {
                node.ReachableNodes.Clear();
                node.UndirectGen = _gen;
            }
        }

        private void InitGroup(Node node, List<Node> groupNodes)
        {
            var newGroup = _nodeGroupFactory.MakeNodeGroup(node.NodeGroupID);
            newGroup.Initialize(node);

            foreach (var groupNode in groupNodes)
            {
                groupNode.NodeGroup = newGroup;
            }

            newGroup.LoadNodes(groupNodes);
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
                    allNodes.Add(reachable);
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

        private void SendFullStateImmediate(IPlayerSession player)
        {
            var msg = new NodeVis.MsgData();
        }
    }

    internal abstract class NodeGroupDebugVisMsg : EntityEventArgs
    {
    }
}
