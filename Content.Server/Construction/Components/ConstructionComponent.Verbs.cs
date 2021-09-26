using Content.Shared.ActionBlocker;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Construction.Components
{
    public partial class ConstructionComponent
    {
        [Verb]
        public sealed class DeconstructibleVerb : Verb<ConstructionComponent>
        {
            protected override void GetData(IEntity user, ConstructionComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (((component.Target != null) && (component.Target.Name == component.DeconstructionNodeIdentifier)) ||
                    ((component.Node != null) && (component.Node.Name == component.DeconstructionNodeIdentifier)))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.CategoryData = VerbCategories.Construction;
                data.Text = Loc.GetString("deconstructible-verb-get-data-text");
                data.IconTexture = "/Textures/Interface/VerbIcons/rotate_ccw.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, ConstructionComponent component)
            {
                component.SetNewTarget(component.DeconstructionNodeIdentifier);
                if (component.Target == null)
                {
                    // Maybe check, but on the flip-side a better solution might be to not make it undeconstructible in the first place, no?
                    component.Owner.PopupMessage(user, Loc.GetString("deconstructible-verb-activate-no-target-text"));
                }
                else
                {
                    component.Owner.PopupMessage(user, Loc.GetString("deconstructible-verb-activate-text"));
                }
            }
        }
    }
}
