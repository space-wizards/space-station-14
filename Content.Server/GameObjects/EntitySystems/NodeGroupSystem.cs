using System.Collections.Generic;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class NodeGroupSystem : EntitySystem
    {
        private readonly HashSet<INodeGroup> _dirtyNodeGroups = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NodeContainerComponent, SnapGridPositionChangedEvent>(OnSnapGridPositionChanged);
        }

        private void OnSnapGridPositionChanged(EntityUid uid, NodeContainerComponent component, SnapGridPositionChangedEvent args)
        {
            foreach (var node in component.Nodes.Values)
            {
                node.OnSnapGridMove();
            }
        }

        public void AddDirtyNodeGroup(INodeGroup nodeGroup)
        {
            _dirtyNodeGroups.Add(nodeGroup);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var group in _dirtyNodeGroups)
            {
                group.RemakeGroup();
            }

            _dirtyNodeGroups.Clear();
        }
    }
}
