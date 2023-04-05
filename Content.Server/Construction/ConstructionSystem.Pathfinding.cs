using Content.Server.Construction.Components;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;

namespace Content.Server.Construction
{
    public sealed partial class ConstructionSystem
    {
        /// <summary>
        ///     Sets or clears a pathfinding target node for a given construction entity.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="targetNodeId">The target node to pathfind, or null to clear the current pathfinding node.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>Whether we could set/clear the pathfinding target node.</returns>
        public bool SetPathfindingTarget(EntityUid uid, string? targetNodeId, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return false;

            // Clear current target, just in case.
            ClearPathfinding(uid, construction);

            // Null means clear pathfinding target only.
            if (targetNodeId == null)
            {
                return true;
            }

            if (GetCurrentGraph(uid, construction) is not {} graph)
                return false;

            if (GetNodeFromGraph(graph, construction.Node) is not { } node)
                return false;

            if (GetNodeFromGraph(graph, targetNodeId) is not {} targetNode)
                return false;

            return UpdatePathfinding(uid, graph, node, targetNode, GetCurrentEdge(uid, construction), construction);
        }

        /// <summary>
        ///     Updates the pathfinding state for the current construction state of an entity.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>Whether we could update the pathfinding state correctly.</returns>
        public bool UpdatePathfinding(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return false;

            if (construction.TargetNode is not {} targetNodeId)
                return false;

            if (GetCurrentGraph(uid, construction) is not {} graph
                || GetNodeFromGraph(graph, construction.Node) is not {} node
                || GetNodeFromGraph(graph, targetNodeId) is not {} targetNode)
                return false;

            return UpdatePathfinding(uid, graph, node, targetNode, GetCurrentEdge(uid, construction), construction);
        }

        /// <summary>
        ///     Internal version of <see cref="UpdatePathfinding"/>, which expects a valid construction state and
        ///     actually performs the pathfinding update logic.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="graph">The construction graph the entity is at.</param>
        /// <param name="currentNode">The current construction node the entity is at.</param>
        /// <param name="targetNode">The target node we are trying to reach on the graph.</param>
        /// <param name="currentEdge">The current edge the entity is at, or null if none.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>Whether we could update the pathfinding state correctly.</returns>
        private bool UpdatePathfinding(EntityUid uid, ConstructionGraphPrototype graph,
            ConstructionGraphNode currentNode, ConstructionGraphNode targetNode,
            ConstructionGraphEdge? currentEdge,
            ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return false;

            construction.TargetNode = targetNode.Name;

            // Check if we reached the target node.
            if (currentNode == targetNode)
            {
                ClearPathfinding(uid, construction);
                return true;
            }

            // If we don't have a path, generate it.
            if (construction.NodePathfinding == null)
            {
                var path = graph.PathId(currentNode.Name, targetNode.Name);

                if (path == null || path.Length == 0)
                {
                    // No path.
                    ClearPathfinding(uid, construction);
                    return false;
                }

                construction.NodePathfinding = new Queue<string>(path);
            }

            // If the next pathfinding node is the one we're at, dequeue it.
            if (construction.NodePathfinding.Peek() == currentNode.Name)
            {
                construction.NodePathfinding.Dequeue();
            }

            if (currentEdge != null && construction.TargetEdgeIndex is {} targetEdgeIndex)
            {
                if (currentNode.Edges.Count >= targetEdgeIndex)
                {
                    // Target edge is incorrect.
                    construction.TargetEdgeIndex = null;
                }
                else if (currentNode.Edges[targetEdgeIndex] != currentEdge)
                {
                    // We went the wrong way, clean up!
                    ClearPathfinding(uid, construction);
                    return false;
                }
            }

            if (construction.EdgeIndex == null
                && construction.TargetEdgeIndex == null
                && construction.NodePathfinding != null)
                construction.TargetEdgeIndex = (currentNode.GetEdgeIndex(construction.NodePathfinding.Peek()));

            return true;
        }

        /// <summary>
        ///     Clears the pathfinding targets on a construction entity.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        public void ClearPathfinding(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return;

            construction.TargetNode = null;
            construction.TargetEdgeIndex = null;
            construction.NodePathfinding = null;
        }
    }
}
