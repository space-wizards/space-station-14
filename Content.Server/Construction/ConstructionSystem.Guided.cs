using Content.Server.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Construction
{
    public partial class ConstructionSystem
    {
        private void InitializeGuided()
        {
            SubscribeLocalEvent<ConstructionComponent, GetOtherVerbsEvent>(AddDeconstructVerb);
            SubscribeLocalEvent<ConstructionComponent, ExaminedEvent>(HandleConstructionExamined);
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
    }
}
