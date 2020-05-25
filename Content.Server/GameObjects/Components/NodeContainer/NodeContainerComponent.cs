using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer
{
    /// <summary>
    ///     Creates and maintains a set of <see cref="INode"/>s.
    /// </summary>
    [RegisterComponent]
    public class NodeContainerComponent : Component
    {
        public override string Name => "NodeContainer";

        [ViewVariables]
        public IReadOnlyList<INode> Nodes => _nodes;
        private List<INode> _nodes = new List<INode>();

#pragma warning disable 649
        [Dependency] private readonly INodeFactory _nodeFactory;
#pragma warning restore 649

        /// <summary>
        ///     A set of <see cref="NodeGroupID"/>s and <see cref="INode"/> implementation names
        ///     to be created and held in this container.
        /// </summary>
        [ViewVariables]
        private Dictionary<NodeGroupID, List<string>> _nodeTypes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _nodeTypes, "nodeTypes", new Dictionary<NodeGroupID, List<string>> { });
        }

        public override void Initialize()
        {
            base.Initialize();
            foreach (var nodeType in _nodeTypes)
            {
                var nodeGroupID = nodeType.Key;
                foreach (var nodeName in nodeType.Value)
                {
                    _nodes.Add(MakeNewNode(nodeName, nodeGroupID, Owner));
                }
            }
            foreach (var node in _nodes)
            {
                node.OnContainerInitialize();
            }
        }

        public override void OnRemove()
        {
            foreach (var node in _nodes)
            {
                node.OnContainerRemove();
            }
            _nodes = null;
            //nodes should now be GC-able
            base.OnRemove();
        }

        private INode MakeNewNode(string nodeName, NodeGroupID groupID, IEntity owner)
        {
            return _nodeFactory.MakeNode(nodeName, groupID, owner);
        }
    }
}
