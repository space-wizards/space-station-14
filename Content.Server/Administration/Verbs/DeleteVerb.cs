#nullable enable
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Verbs
{
    [GlobalVerb]
    public class DeleteVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            var groupController = IoCManager.Resolve<IConGroupController>();

            if (!user.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            if (!groupController.CanCommand(actor.PlayerSession, "deleteentity"))
            {
                return;
            }

            data.Text = Loc.GetString("Delete");
            data.CategoryData = VerbCategories.Debug;
            data.Visibility = VerbVisibility.Visible;
            data.IconTexture = "/Textures/Interface/VerbIcons/delete.svg.192dpi.png";
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();

            if (!user.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            if (!groupController.CanCommand(actor.PlayerSession, "deleteentity"))
            {
                return;
            }

            target.Delete();
        }
    }
}
