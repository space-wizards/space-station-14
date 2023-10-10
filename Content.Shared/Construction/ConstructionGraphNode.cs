using System.Diagnostics.CodeAnalysis;
using Content.Shared.Construction.NodeEntities;
using Content.Shared.Construction.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction
{
    [Serializable]
    [DataDefinition]
    public sealed partial class ConstructionGraphNode
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

        [DataField("transform")]
        public IGraphTransform[] TransformLogic = Array.Empty<IGraphTransform>();

        [DataField("entity", customTypeSerializer: typeof(GraphNodeEntitySerializer), serverOnly:true)]
        public IGraphNodeEntity Entity { get; private set; } = new NullNodeEntity();

        /// <summary>
        ///     Ignore requests to change the entity if the entity's current prototype inherits from specified replacement 
        /// </summary>
        /// <remarks>
        ///     When this bool is true and a construction node specifies that the current entity should be replaced with a new entity, if the 
        ///     current entity has an entity prototype which inherits from the replacement entity prototype, entity replacement will not occur.
        ///     E.g., if an entity with the 'AirlockCommand' prototype was to be replaced with a new entity that had the 'Airlock' prototype, 
        ///     and 'DoNotReplaceInheritingEntities' was true, the entity would not be replaced because 'AirlockCommand' is derived from 'Airlock' 
        ///     This will largely be used for construction graphs which have removeable upgrades, such as hacking protections for airlocks,
        ///     so that the upgrades can be removed and you can return to the last primary construction step without replacing the entity
        /// </remarks>
        [DataField("doNotReplaceInheritingEntities")]
        public bool DoNotReplaceInheritingEntities = false;

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
