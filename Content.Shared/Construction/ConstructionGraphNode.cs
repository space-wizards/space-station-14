using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Construction
{
    [Serializable]
    [DataDefinition]
    public class ConstructionGraphNode
    {
        [DataField("actions", serverOnly: true)]
        private List<IGraphAction> _actions = new();

        [DataField("edges")]
        private List<ConstructionGraphEdge> _edges = new();

        [ViewVariables]
        [DataField("node", required: true)]
        public string Name { get; private set; } = default!;

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphEdge> Edges => _edges;

        [ViewVariables]
        public IReadOnlyList<IGraphAction> Actions => _actions;

        [ViewVariables]
        [DataField("entity")]
        public string? Entity { get; private set; }

        public ConstructionGraphEdge? GetEdge(string target)
        {
            foreach (var edge in _edges)
            {
                if (edge.Target == target)
                    return edge;
            }

            return null;
        }

        public bool TryGetEdge(string target, [NotNullWhen(true)] out ConstructionGraphEdge? edge)
        {
            return (edge = GetEdge(target)) != null;
        }
    }
}
