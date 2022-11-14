using Content.Server.Construction.Components;
using Content.Server.DoAfter;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Construction
{
    /// <summary>
    /// The server-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ConstructionSystem : SharedConstructionSystem
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly ToolSystem _toolSystem = default!;

        private const string SawmillName = "Construction";
        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = _logManager.GetSawmill(SawmillName);

            InitializeComputer();
            InitializeGraphs();
            InitializeGuided();
            InitializeInteractions();
            InitializeInitial();
            InitializeMachines();

            SubscribeLocalEvent<ConstructionComponent, ComponentInit>(OnConstructionInit);
            SubscribeLocalEvent<ConstructionComponent, ComponentStartup>(OnConstructionStartup);
        }

        private void OnConstructionInit(EntityUid uid, ConstructionComponent construction, ComponentInit args)
        {
            if (GetCurrentGraph(uid, construction) is not {} graph)
            {
                _sawmill.Warning($"Prototype {EntityManager.GetComponent<MetaDataComponent>(construction.Owner).EntityPrototype?.ID}'s construction component has an invalid graph specified.");
                return;
            }

            if (GetNodeFromGraph(graph, construction.Node) is not {} node)
            {
                _sawmill.Warning($"Prototype {EntityManager.GetComponent<MetaDataComponent>(construction.Owner).EntityPrototype?.ID}'s construction component has an invalid node specified.");
                return;
            }

            ConstructionGraphEdge? edge = null;
            if (construction.EdgeIndex is {} edgeIndex)
            {
                if (GetEdgeFromNode(node, edgeIndex) is not {} currentEdge)
                {
                    _sawmill.Warning($"Prototype {EntityManager.GetComponent<MetaDataComponent>(construction.Owner).EntityPrototype?.ID}'s construction component has an invalid edge index specified.");
                    return;
                }

                edge = currentEdge;
            }

            if (construction.TargetNode is {} targetNodeId)
            {
                if (GetNodeFromGraph(graph, targetNodeId) is not { } targetNode)
                {
                    _sawmill.Warning($"Prototype {EntityManager.GetComponent<MetaDataComponent>(construction.Owner).EntityPrototype?.ID}'s construction component has an invalid target node specified.");
                    return;
                }

                UpdatePathfinding(uid, graph, node, targetNode, edge, construction);
            }
        }

        private void OnConstructionStartup(EntityUid uid, ConstructionComponent construction, ComponentStartup args)
        {
            if (GetCurrentNode(uid, construction) is not {} node)
                return;

            PerformActions(uid, null, node.Actions);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateInteractions();
        }
    }
}
