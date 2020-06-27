using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    public interface INodeFactory
    {
        void Initialize();

        Node MakeNode(string nodeName, NodeGroupID groupID, IEntity owner);
    }

    public class NodeFactory : INodeFactory
    {
        private readonly Dictionary<string, Type> _groupTypes = new Dictionary<string, Type>();

#pragma warning disable 649
        [Dependency] private readonly IReflectionManager _reflectionManager;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory;
#pragma warning restore 649

        public void Initialize()
        {
            var nodeTypes = _reflectionManager.GetAllChildren<Node>();
            foreach (var nodeType in nodeTypes)
            {
                var att = nodeType.GetCustomAttribute<NodeAttribute>();
                if (att != null)
                {
                    _groupTypes.Add(att.Name, nodeType);
                }
            }
        }

        public Node MakeNode(string nodeName, NodeGroupID groupID, IEntity owner)
        {
            if (_groupTypes.TryGetValue(nodeName, out var type))
            {
                var newNode = _typeFactory.CreateInstance<Node>(type);
                newNode.Initialize(groupID, owner);
                return newNode;
            }
            throw new ArgumentException($"{nodeName} did not have an associated {nameof(Node)}.");
        }
    }
}
