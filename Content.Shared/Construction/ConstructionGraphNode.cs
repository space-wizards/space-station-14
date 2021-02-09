using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Construction
{
    [Serializable]
    [YamlDefinition]
    public class ConstructionGraphNode
    {
        [YamlField("actions", serverOnly: true)]
        private List<IGraphAction> _actions = new();
        [YamlField("edges")]
        private List<ConstructionGraphEdge> _edges = new();

        [ViewVariables]
        [YamlField("node")]
        public string Name { get; private set; }

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphEdge> Edges => _edges;

        [ViewVariables]
        public IReadOnlyList<IGraphAction> Actions => _actions;

        [ViewVariables]
        [YamlField("entity")]
        public string Entity { get; private set; }

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
