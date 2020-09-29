using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;
using ObjectSerializer = Robust.Shared.Serialization.ObjectSerializer;

namespace Content.Shared.Construction
{
    [Serializable, NetSerializable]
    public class ConstructionGraphNode
    {
        private List<ConstructionGraphEdge> _edges = new List<ConstructionGraphEdge>();

        [ViewVariables]
        public string Name { get; private set; }

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphEdge> Edges => _edges;

        [ViewVariables]
        public string Entity { get; private set; }

        [ViewVariables]
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
