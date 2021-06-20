using System.Collections.Generic;
using Content.Server.NodeContainer.NodeGroups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.NodeContainer.EntitySystems
{
    [UsedImplicitly]
    public class NodeGroupSystem : EntitySystem
    {
        private readonly HashSet<INodeGroup> _dirtyNodeGroups = new();

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
