using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;

namespace Content.Shared.Construction
{
    public abstract partial class SharedConstructionSystem
    {
        /// <summary>
        ///     Gets the current construction graph of an entity, or null.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The current construction graph of an entity or null if invalid. Also returns null if the entity
        ///          does not have a <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>An entity with a valid construction state will always have a valid graph.</remarks>
        public ConstructionGraphPrototype? GetCurrentGraph(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            // If the set graph prototype does not exist, also return null. This could be due to admemes changing values
            // in ViewVariables, so even though the construction state is invalid, just return null.
            return PrototypeManager.TryIndex(construction.Graph, out ConstructionGraphPrototype? graph) ? graph : null;
        }

        /// <summary>
        ///     Gets the construction graph node the entity is currently at, or null.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The current construction graph node the entity is currently at, or null if invalid. Also returns
        ///          null if the entity does not have a <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>An entity with a valid construction state will always be at a valid node.</remarks>
        public ConstructionGraphNode? GetCurrentNode(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            if (construction.Node is not { } nodeIdentifier)
                return null;

            return GetCurrentGraph(uid, construction) is not { } graph ? null : GetNodeFromGraph(graph, nodeIdentifier);
        }

        /// <summary>
        ///     Gets the construction graph edge the entity is currently at, or null.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The construction graph edge the entity is currently at, if any. Also returns null if the entity
        ///          does not have a <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>An entity with a valid construction state might not always be at an edge.</remarks>
        public ConstructionGraphEdge? GetCurrentEdge(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            if (construction.EdgeIndex is not { } edgeIndex)
                return null;

            return GetCurrentNode(uid, construction) is not { } node ? null : GetEdgeFromNode(node, edgeIndex);
        }

        /// <summary>
        ///     Gets the construction graph node the entity's construction pathfinding is currently targeting, if any.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The construction graph node the entity's construction pathfinding is currently targeting, or null
        ///          if it's not currently targeting any node. Also returns null if the entity does not have a
        ///          <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>Target nodes are entirely optional and only used for pathfinding purposes.</remarks>
        public ConstructionGraphNode? GetTargetNode(EntityUid uid, ConstructionComponent? construction)
        {
            if (!Resolve(uid, ref construction))
                return null;

            if (construction.TargetNode is not { } targetNodeId)
                return null;

            if (GetCurrentGraph(uid, construction) is not { } graph)
                return null;

            return GetNodeFromGraph(graph, targetNodeId);
        }

        /// <summary>
        ///     Gets the construction graph edge the entity's construction pathfinding is currently targeting, if any.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The construction graph edge the entity's construction pathfinding is currently targeting, or null
        ///          if it's not currently targeting any edge. Also returns null if the entity does not have a
        ///          <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>Target edges are entirely optional and only used for pathfinding purposes. The targeted edge will
        ///          be an edge on the current construction node the entity is at.</remarks>
        public ConstructionGraphEdge? GetTargetEdge(EntityUid uid, ConstructionComponent? construction)
        {
            if (!Resolve(uid, ref construction))
                return null;

            if (construction.TargetEdgeIndex is not { } targetEdgeIndex)
                return null;

            if (GetCurrentNode(uid, construction) is not { } node)
                return null;

            return GetEdgeFromNode(node, targetEdgeIndex);
        }

        /// <summary>
        ///     Gets a node from a construction graph given its identifier.
        /// </summary>
        /// <param name="graph">The construction graph where to get the node.</param>
        /// <param name="id">The identifier that corresponds to the node.</param>
        /// <returns>The node that corresponds to the identifier, or null if it doesn't exist.</returns>
        public ConstructionGraphNode? GetNodeFromGraph(ConstructionGraphPrototype graph, string id)
        {
            return graph.Nodes.TryGetValue(id, out var node) ? node : null;
        }

        /// <summary>
        ///     Gets an edge from a construction node given its index.
        /// </summary>
        /// <param name="node">The construction node where to get the edge.</param>
        /// <param name="index">The index or position of the edge on the node.</param>
        /// <returns>The edge on that index in the construction node, or null if none.</returns>
        public ConstructionGraphEdge? GetEdgeFromNode(ConstructionGraphNode node, int index)
        {
            return node.Edges.Count > index ? node.Edges[index] : null;
        }
    }
}
