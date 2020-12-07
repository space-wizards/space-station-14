using System;
using System.Collections.Generic;
using Content.Shared.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;
using ObjectSerializer = Robust.Shared.Serialization.ObjectSerializer;

namespace Content.Shared.Construction
{
    [Serializable]
    public class ConstructionGraphNode
    {
        private List<IGraphAction> _actions = new();
        private List<ConstructionGraphEdge> _edges = new();

        [ViewVariables]
        public string Name { get; private set; }

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphEdge> Edges => _edges;

        [ViewVariables]
        public IReadOnlyList<IGraphAction> Actions => _actions;

        [ViewVariables]
        public string Entity { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            var moduleManager = IoCManager.Resolve<IModuleManager>();

            serializer.DataField(this, x => x.Name, "node", string.Empty);
            serializer.DataField(this, x => x.Entity, "entity",string.Empty);
            if (!moduleManager.IsServerModule) return;
            serializer.DataField(ref _actions, "actions", new List<IGraphAction>());
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

        public ConstructionGraphEdge GetEdge(string target)
        {
            foreach (var edge in _edges)
            {
                if (edge.Target == target)
                    return edge;
            }

            return null;
        }
    }
}
