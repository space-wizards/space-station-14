using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Construction
{
    public partial class ConstructionComponent
    {
        [Verb]
        public sealed class DeconstructibleVerb : Verb<ConstructionComponent>
        {
            protected override void GetData(IEntity user, ConstructionComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
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
                data.Text = Loc.GetString("Begin deconstructing");
                data.IconTexture = "/Textures/Interface/VerbIcons/rotate_ccw.svg.96dpi.png";
            }

            protected override void Activate(IEntity user, ConstructionComponent component)
            {
                component.SetNewTarget(component.DeconstructionNodeIdentifier);
                if (component.Target == null)
                {
                    // Maybe check, but on the flip-side a better solution might be to not make it undeconstructible in the first place, no?
                    component.Owner.PopupMessage(user, Loc.GetString("There is no way to deconstruct this."));
                }
                else
                {
                    component.Owner.PopupMessage(user, Loc.GetString("Examine to see instructions."));
                }
            }
        }
    }
}
