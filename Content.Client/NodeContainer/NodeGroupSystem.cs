using System.Collections.Generic;
using System.Linq;
using Content.Shared.NodeContainer;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.NodeContainer
{
    [UsedImplicitly]
    public sealed class NodeGroupSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClientResourceCache _resourceCache = default!;

        public bool VisEnabled { get; private set; }

        public Dictionary<int, NodeVis.GroupData> Groups { get; } = new();
        public HashSet<string> Filtered { get; } = new();

        public Dictionary<EntityUid, (NodeVis.GroupData group, NodeVis.NodeDatum node)[]>
            Entities { get; private set; } = new();

        public Dictionary<(int group, int node), NodeVis.NodeDatum> NodeLookup { get; private set; } = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<NodeVis.MsgData>(DataMsgHandler);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _overlayManager.RemoveOverlay<NodeVisualizationOverlay>();
        }

        private void DataMsgHandler(NodeVis.MsgData ev)
        {
            if (!VisEnabled)
                return;

            foreach (var deletion in ev.GroupDeletions)
            {
                Groups.Remove(deletion);
            }

            foreach (var group in ev.Groups)
            {
                Groups.Add(group.NetId, group);
            }

            foreach (var (groupId, debugData) in ev.GroupDataUpdates)
            {
                if (Groups.TryGetValue(groupId, out var group))
                {
                    group.DebugData = debugData;
                }
            }

            Entities = Groups.Values
                .SelectMany(g => g.Nodes, (data, nodeData) => (data, nodeData))
                .GroupBy(n => GetEntity(n.nodeData.Entity))
                .ToDictionary(g => g.Key, g => g.ToArray());

            NodeLookup = Groups.Values
                .SelectMany(g => g.Nodes, (data, nodeData) => (data, nodeData))
                .ToDictionary(n => (n.data.NetId, n.nodeData.NetId), n => n.nodeData);
        }

        public void SetVisEnabled(bool enabled)
        {
            VisEnabled = enabled;

            RaiseNetworkEvent(new NodeVis.MsgEnable(enabled));

            if (enabled)
            {
                var overlay = new NodeVisualizationOverlay(
                    this,
                    _entityLookup,
                    _mapManager,
                    _inputManager,
                    _resourceCache,
                    EntityManager);

                _overlayManager.AddOverlay(overlay);
            }
            else
            {
                Groups.Clear();
                Entities.Clear();
            }
        }
    }
}
