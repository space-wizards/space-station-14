using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    /// <summary>
    ///     Maintains a unique set of <see cref="INodeGroup"/>s, and remakes the node graphs
    ///     on Dirty groups periodically.
    /// </summary>
    public interface INodeGroupManager
    {
        /// <summary>
        ///     Add an <see cref="INodeGroup"/> to be updated.
        /// </summary>
        void AddNodeGroup(INodeGroup nodeGroup);

        void RemoveGroup(INodeGroup nodeGroup);

        void Update(float frameTime);
    }

    public class NodeGroupManager : INodeGroupManager
    {
        /// <summary>
        ///     The set of <see cref="INodeGroup"/>s that this manager is updating.
        /// </summary>
        private readonly HashSet<INodeGroup> _nodeGroups = new HashSet<INodeGroup>();

        /// <summary>
        ///     <see cref="INodeGroup"/>s that are added mid-<see cref="Update"/> are stored
        ///     here to avoid modifying <see cref="_nodeGroups"/> while iterating through it.
        /// </summary>
        private readonly List<INodeGroup> _queuedNewNodeGroups = new List<INodeGroup>();

        /// <summary>
        ///     If this is in the middle of an <see cref="Update"/> and should not modify
        ///     <see cref="_nodeGroups"/>.
        /// </summary>
        private bool _updating = false;

        private float _accumulatedFrameTime;

        public void AddNodeGroup(INodeGroup nodeGroup)
        {
            if (_updating)
            {
                _queuedNewNodeGroups.Add(nodeGroup);
            }
            else
            {
                _nodeGroups.Add(nodeGroup);
            }
        }

        public void RemoveGroup(INodeGroup nodeGroup)
        {
            _nodeGroups.Remove(nodeGroup);
            _queuedNewNodeGroups.Remove(nodeGroup);
        }

        public void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime <= 1)
                return;
            _accumulatedFrameTime = 0;

            _updating = true;
            var groupsToRemove = new List<INodeGroup>();
            foreach (var group in _nodeGroups)
            {
                if (group.Dirty)
                {
                    group.RemakeGroup(); //causes more groups to be made and added
                    groupsToRemove.Add(group);
                }
            }
            RemoveGroups(groupsToRemove);
            AddGroups(_queuedNewNodeGroups);
            _queuedNewNodeGroups.Clear();
            _updating = false;
        }

        private void RemoveGroups(IEnumerable<INodeGroup> groups)
        {
            foreach (var group in groups)
            {
                _nodeGroups.Remove(group);
            }
        }

        private void AddGroups(IEnumerable<INodeGroup> groups)
        {
            foreach (var group in groups)
            {
                _nodeGroups.Add(group);
            }
        }
    }
}
