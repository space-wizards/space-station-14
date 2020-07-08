using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.NodeContainer
{
    public class NodeTypeSerializer : YamlObjectSerializer.TypeSerializer
    {
        private readonly INodeFactory _nodeFactory;

        public NodeTypeSerializer()
        {
            _nodeFactory = IoCManager.Resolve<INodeFactory>();
        }

        public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
        {
            var mapping = (YamlMappingNode) node;
            var nodeType = mapping.GetNode("type").ToString();
            var newNode = _nodeFactory.MakeNode(nodeType);
            newNode.ExposeData(YamlObjectSerializer.NewReader((YamlMappingNode) node));
            return newNode;
        }

        public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
