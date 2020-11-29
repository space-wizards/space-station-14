using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    /// <summary>
    ///     Maintains a set of <see cref="INodeGroup"/>s that need to be remade with <see cref="INodeGroup.RemakeGroup"/>.
    ///     Defers remaking to reduce recalculations when a group is altered multiple times in a frame.
    /// </summary>
    public interface INodeGroupManager
    {
        /// <summary>
        ///     Queue up an <see cref="INodeGroup"/> to be remade.
        /// </summary>
        void AddDirtyNodeGroup(INodeGroup nodeGroup);

        void Update(float frameTime);
    }

    public class NodeGroupManager : INodeGroupManager
    {
        private readonly HashSet<INodeGroup> _dirtyNodeGroups = new();

        public void AddDirtyNodeGroup(INodeGroup nodeGroup)
        {
            _dirtyNodeGroups.Add(nodeGroup);
        }

        public void Update(float frameTime)
        {
            foreach (var group in _dirtyNodeGroups)
            {
                group.RemakeGroup();
            }
            _dirtyNodeGroups.Clear();
        }
    }
}
