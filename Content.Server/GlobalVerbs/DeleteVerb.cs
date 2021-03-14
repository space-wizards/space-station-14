#nullable enable
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
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

            if (!user.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            if (!groupController.CanCommand(actor.playerSession, "deleteentity"))
            {
                return;
            }

            data.Text = Loc.GetString("Delete");
            data.CategoryData = VerbCategories.Debug;
            data.Visibility = VerbVisibility.Visible;
            data.IconTexture = "/Textures/Interface/VerbIcons/delete.svg.96dpi.png";
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();

            if (!user.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            if (!groupController.CanCommand(actor.playerSession, "deleteentity"))
            {
                return;
            }

            target.Delete();
        }
    }
}
