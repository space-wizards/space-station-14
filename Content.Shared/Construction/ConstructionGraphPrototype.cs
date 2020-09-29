using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    [Prototype("constructionGraph")]
    public class ConstructionGraph : IPrototype, IIndexedPrototype
    {
        private Dictionary<string, ConstructionGraphNode> _nodes = new Dictionary<string, ConstructionGraphNode>();
        public string ID { get; private set; }
        public IReadOnlyDictionary<string, ConstructionGraphNode> Nodes => _nodes;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => x.ID, "id", string.Empty);
            // var graph= serializer.ReadDataField("graph", new List<ConstructionGraphNode>());
            // _nodes = graph.ToDictionary(node => node.Name, node => node);

            if (!mapping.TryGetNode("graph", out YamlSequenceNode graphMapping)) return;

            foreach (var yamlNode in graphMapping)
            {
                var childMapping = (YamlMappingNode) yamlNode;
                var node = new ConstructionGraphNode();
                node.LoadFrom(childMapping);
                _nodes[node.Name] = node;
            }
        }
    }
}
