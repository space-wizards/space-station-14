using Content.Server.Construction.Components;
using Content.Server.DoAfter;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Construction
{
    /// <summary>
    /// The server-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    public partial class ConstructionSystem : SharedConstructionSystem
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly ToolSystem _toolSystem = default!;

        private const string SawmillName = "Construction";
        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = _logManager.GetSawmill(SawmillName);

            InitializeGraphs();
            InitializeSteps();
            InitializeInitial();

            SubscribeLocalEvent<ConstructionComponent, ComponentInit>(OnConstructionInit);
            SubscribeLocalEvent<ConstructionComponent, ComponentStartup>(OnConstructionStartup);
            SubscribeLocalEvent<ConstructionComponent, GetOtherVerbsEvent>(AddDeconstructVerb);
            SubscribeLocalEvent<ConstructionComponent, ExaminedEvent>(HandleConstructionExamined);
        }

        private void OnConstructionInit(EntityUid uid, ConstructionComponent construction, ComponentInit args)
        {
            if (GetCurrentGraph(uid, construction) is not {} graph)
            {
                _sawmill.Warning($"Prototype {construction.Owner.Prototype?.ID}'s construction component has an invalid graph specified.");
                return;
            }

            if (GetNodeFromGraph(graph, construction.Node) is not {} node)
            {
                _sawmill.Warning($"Prototype {construction.Owner.Prototype?.ID}'s construction component has an invalid node specified.");
                return;
            }

            ConstructionGraphEdge? edge = null;
            if (construction.EdgeIndex is {} edgeIndex)
            {
                if (GetEdgeFromNode(node, edgeIndex) is not {} currentEdge)
                {
                    _sawmill.Warning($"Prototype {construction.Owner.Prototype?.ID}'s construction component has an invalid edge index specified.");
                    return;
                }

                edge = currentEdge;
            }

            if (construction.TargetNode is {} targetNodeId)
            {
                if (GetNodeFromGraph(graph, targetNodeId) is not { } targetNode)
                {
                    _sawmill.Warning($"Prototype {construction.Owner.Prototype?.ID}'s construction component has an invalid target node specified.");
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

        private void AddDeconstructVerb(EntityUid uid, ConstructionComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess)
                return;

            if (component.TargetNode == component.DeconstructionNode ||
                component.Node == component.DeconstructionNode)
                return;

            Verb verb = new();
            //verb.Category = VerbCategories.Construction;
            //TODO VERBS add more construction verbs? Until then, removing construction category
            verb.Text = Loc.GetString("deconstructible-verb-begin-deconstruct");
            verb.IconTexture = "/Textures/Interface/hammer_scaled.svg.192dpi.png";

            verb.Act = () =>
            {
                SetPathfindingTarget(uid, component.DeconstructionNode, component);
                if (component.TargetNode == null)
                {
                    // Maybe check, but on the flip-side a better solution might be to not make it undeconstructible in the first place, no?
                    component.Owner.PopupMessage(args.User, Loc.GetString("deconstructible-verb-activate-no-target-text"));
                }
                else
                {
                    component.Owner.PopupMessage(args.User, Loc.GetString("deconstructible-verb-activate-text"));
                }
            };

            args.Verbs.Add(verb);
        }

        private void HandleConstructionExamined(EntityUid uid, ConstructionComponent component, ExaminedEvent args)
        {
            if (GetTargetNode(uid, component) is {} target)
            {
                args.PushMarkup(Loc.GetString(
                    "construction-component-to-create-header",
                    ("targetName", target.Name)) + "\n");
            }

            if (component.EdgeIndex == null && GetTargetEdge(uid, component) is {} targetEdge)
            {
                var preventStepExamine = false;

                foreach (var condition in targetEdge.Conditions)
                {
                    preventStepExamine |= condition.DoExamine(args);
                }

                if (!preventStepExamine)
                    targetEdge.Steps[0].DoExamine(args);
                return;
            }

            if (GetCurrentEdge(uid, component) is {} edge)
            {
                var preventStepExamine = false;

                foreach (var condition in edge.Conditions)
                {
                    preventStepExamine |= condition.DoExamine(args);
                }

                if (preventStepExamine) return;
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateInteractions();
        }
    }
}
