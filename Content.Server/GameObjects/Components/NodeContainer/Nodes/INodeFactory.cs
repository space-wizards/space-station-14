using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    public interface INodeFactory
    {
        void Initialize();

        INode MakeNode(string nodeName, NodeGroupID groupID, NodeContainerComponent container);
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
                var att = (NodeAttribute) Attribute.GetCustomAttribute(nodeType, typeof(NodeAttribute));
                if (att != null)
                {
                    _groupTypes.Add(att.Name, nodeType);
                }
            }
        }

        public INode MakeNode(string nodeName, NodeGroupID groupID, NodeContainerComponent container)
        {
            if (_groupTypes.TryGetValue(nodeName, out var type))
            {
                return (INode) _typeFactory.CreateInstance(type, new object[] { groupID, container });
            }
            throw new ArgumentException($"{nodeName} did not have an associated {nameof(INode)}.");
        }
    }
}
