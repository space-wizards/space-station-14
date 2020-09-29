using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using ObjectSerializer = Robust.Shared.Serialization.ObjectSerializer;

namespace Content.Shared.Construction
{
    [Serializable, NetSerializable]
    public class ConstructionGraphNode
    {
        private List<ConstructionGraphEdge> _edges = new List<ConstructionGraphEdge>();

        public string Name { get; private set; }
        public IReadOnlyList<ConstructionGraphEdge> Edges => _edges;
        public string Entity { get; private set; }
        public string SpriteState { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Name, "node", string.Empty);
            serializer.DataField(this, x => x.Entity, "entity",string.Empty);
            serializer.DataField(this, x => x.SpriteState, "spriteState", string.Empty);
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            ExposeData(serializer);

            if (!mapping.TryGetNode("edges", out YamlSequenceNode edgesMapping)) return;

            foreach (var yamlNode in edgesMapping)
            {
                var edgeMapping = (YamlMappingNode) yamlNode;
                var edge = new ConstructionGraphEdge();
                edge.LoadFrom(edgeMapping);
                _edges.Add(edge);
            }
        }
    }
}
