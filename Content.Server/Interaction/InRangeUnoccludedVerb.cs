using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Interaction
{
    [GlobalVerb]
    public class InRangeUnoccludedVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            if (!user.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            var groupController = IoCManager.Resolve<IConGroupController>();
            if (!groupController.CanCommand(actor.PlayerSession, "inrangeunoccluded"))
            {
                return;
            }

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("in-range-unoccluded-verb-get-data-text");
            data.IconTexture = "/Textures/Interface/VerbIcons/information.svg.192dpi.png";
            data.CategoryData = VerbCategories.Debug;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!user.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            var groupController = IoCManager.Resolve<IConGroupController>();
            if (!groupController.CanCommand(actor.PlayerSession, "inrangeunoccluded"))
            {
                return;
            }

            var message = user.InRangeUnOccluded(target)
                ? Loc.GetString("in-range-unoccluded-verb-on-activate-not-occluded")
                : Loc.GetString("in-range-unoccluded-verb-on-activate-occluded");

            target.PopupMessage(user, message);
        }
    }
}
