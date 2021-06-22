using System.Collections.Generic;
using System.Linq;
using Content.Shared.NodeContainer;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.NodeContainer
{
    [UsedImplicitly]
    public sealed class NodeGroupSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IEntityLookup _entityLookup = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public bool VisEnabled { get; private set; }

        public Dictionary<int, NodeVis.GroupData> Groups { get; } = new();
        public HashSet<string> Filtered { get; } = new();

        public Dictionary<EntityUid, (NodeVis.GroupData group, NodeVis.NodeDatum node)[]>
            Entities { get; private set; } = new();

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

            Entities.Clear();

            foreach (var deletion in ev.GroupDeletions)
            {
                Groups.Remove(deletion);
            }

            foreach (var group in ev.Groups)
            {
                Groups.Add(group.NetId, group);
            }

            Entities = Groups.Values
                .SelectMany(c => c.Nodes, (data, nodeData) => (data, nodeData))
                .GroupBy(n => n.nodeData.Entity)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public void SetVisEnabled(bool enabled)
        {
            VisEnabled = enabled;

            RaiseNetworkEvent(new NodeVis.MsgEnable(enabled));

            if (enabled)
            {
                _overlayManager.AddOverlay(new NodeVisualizationOverlay(this, _entityLookup, _mapManager));
            }
            else
            {
                Groups.Clear();
                Entities.Clear();
            }
        }
    }
}
