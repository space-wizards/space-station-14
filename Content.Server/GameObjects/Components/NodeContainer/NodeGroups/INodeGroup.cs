using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    /// <summary>
    ///     Maintains a collection of <see cref="INode"/>s, and performs operations requiring a list of
    ///     all connected <see cref="INode"/>s.
    /// </summary>
    public interface INodeGroup
    {
        public IReadOnlyList<INode> Nodes { get; }

        void AddNode(INode node);

        void RemoveNode(INode node);

        void CombineGroup(INodeGroup newGroup);

        void BeforeCombine();

        void AfterCombine();

        void BeforeRemakeSpread();

        void AfterRemakeSpread();
    }

    [NodeGroup(NodeGroupID.Default)]
    public class BaseNodeGroup : INodeGroup
    {
        [ViewVariables]
        public IReadOnlyList<INode> Nodes => _nodes;
        private readonly List<INode> _nodes = new List<INode>();

        [ViewVariables]
        public int NodeCount => Nodes.Count;

        public static readonly INodeGroup Null = new NullNodeGroup();

        public void AddNode(INode node)
        {
            _nodes.Add(node);
            OnAddNode(node);
        }

        public void RemoveNode(INode node)
        {
            _nodes.Remove(node);
            OnRemoveNode(node);
            RemakeGroup();
        }

        public void CombineGroup(INodeGroup newGroup)
        {
            if (newGroup.Nodes.Count < Nodes.Count)
            {
                newGroup.CombineGroup(this);
                return;
            }
            BeforeCombine();
            newGroup.BeforeCombine();
            foreach (var node in Nodes)
            {
                node.NodeGroup = newGroup;
            }
            AfterCombine();
            newGroup.AfterCombine();
        }

        /// <summary>
        ///     Causes all <see cref="INode"/>s to remake their groups. Called when a <see cref="INode"/> is removed
        ///     and may have split a group in two, so multiple new groups may need to be formed.
        /// </summary>
        private void RemakeGroup()
        {
            BeforeRemake();
            foreach (var node in Nodes)
            {
                node.ClearNodeGroup();
            }
            foreach (var node in Nodes)
            {
                if (node.TryAssignGroupIfNeeded())
                {
                    node.StartSpreadingGroup();
                }
            }
        }

        protected virtual void OnAddNode(INode node) { }
        
        protected virtual void OnRemoveNode(INode node) { }

        protected virtual void BeforeRemake() { }

        protected virtual void AfterRemake() { }

        public virtual void BeforeCombine() { }

        public virtual void AfterCombine() { }

        public virtual void BeforeRemakeSpread() { }

        public virtual void AfterRemakeSpread() { }

        private class NullNodeGroup : INodeGroup
        {
            public IReadOnlyList<INode> Nodes => _nodes;
            private readonly List<INode> _nodes = new List<INode>();
            public void AddNode(INode node) { }
            public void CombineGroup(INodeGroup newGroup) { }
            public void RemoveNode(INode node) { }
            public void BeforeCombine() { }
            public void AfterCombine() { }
            public void BeforeRemakeSpread() { }
            public void AfterRemakeSpread() { }
        }
    }
}
