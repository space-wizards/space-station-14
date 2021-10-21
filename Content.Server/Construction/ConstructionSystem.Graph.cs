using Content.Server.Construction.Components;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Robust.Shared.GameObjects;

namespace Content.Server.Construction
{
    public partial class ConstructionSystem
    {
        private void InitializeGraphs()
        {
        }

        public ConstructionGraphPrototype? GetCurrentGraph(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            return _prototypeManager.TryIndex(construction._graphIdentifier, out ConstructionGraphPrototype? graph) ? graph : null;
        }

        public ConstructionGraphNode? GetCurrentNode(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            if (construction.Node is not {} nodeIdentifier)
                return null;

            if(GetCurrentGraph(uid, construction) is not {} graph)
                return null;

            return graph.Nodes.TryGetValue(nodeIdentifier, out var node) ? node : null;
        }

        public ConstructionGraphEdge? GetCurrentEdge(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            if (construction.Edge is not {} edgeIndex)
                return null;

            if (GetCurrentNode(uid, construction) is not {} node)
                return null;

            return node.Edges.Count > edgeIndex ? node.Edges[edgeIndex] : null;
        }

        public ConstructionGraphStep[]? GetCurrentSteps(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            if (GetCurrentEdge(uid, construction) is not {} edge)
                return null;

            var step = edge.Steps.Count > construction.EdgeStepIndex ? edge.Steps[construction.EdgeStepIndex] : null;

            if (step is NestedConstructionGraphStep nested)
            {
                foreach (var VARIABLE in nested.Steps[construction.EdgeNestedStepProgress])
                {
                    
                }
            }

            return step != null ? new[] { step } : null;
        }

        public void AdvanceStep(EntityUid uid, ConstructionGraphStep step, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return;
        }

        public void ChangeEdge(EntityUid uid, string edge, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return;
        }

        public void ChangeNode(EntityUid uid, string node, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return;
        }

        public void ChangeGraph(EntityUid uid, string graph, string node, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return;
        }
    }
}
