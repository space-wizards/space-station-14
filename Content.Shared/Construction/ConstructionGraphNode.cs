using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction
{
    [Serializable]
    [DataDefinition]
    public sealed class ConstructionGraphNode
    {
        [DataField("actions", serverOnly: true)]
        private IGraphAction[] _actions = Array.Empty<IGraphAction>();

        [DataField("edges")]
        private ConstructionGraphEdge[] _edges = Array.Empty<ConstructionGraphEdge>();

        [DataField("node", required: true)]
        public string Name { get; private set; } = default!;

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphEdge> Edges => _edges;

        [ViewVariables]
        public IReadOnlyList<IGraphAction> Actions => _actions;

        [DataField("entity", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
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

        public int? GetEdgeIndex(string target)
        {
            for (var i = 0; i < _edges.Length; i++)
            {
                var edge = _edges[i];
                if (edge.Target == target)
                    return i;
            }

            return null;
        }

        public bool TryGetEdge(string target, [NotNullWhen(true)] out ConstructionGraphEdge? edge)
        {
            return (edge = GetEdge(target)) != null;
        }
    }
}
