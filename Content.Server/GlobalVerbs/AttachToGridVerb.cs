using Content.Shared.GameObjects.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
{
    [GlobalVerb]
    public class AttachToGridVerb : GlobalVerb
    {
        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            if (user == target)
            {
                return;
            }

            if (!user.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            var groupController = IoCManager.Resolve<IConGroupController>();
            if (!groupController.CanCommand(actor.playerSession, "attachtogrid"))
            {
                return;
            }

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Attach to grid");
            data.CategoryData = VerbCategories.Debug;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!user.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            var groupController = IoCManager.Resolve<IConGroupController>();
            if (!groupController.CanCommand(actor.playerSession, "attachtogrid"))
            {
                return;
            }

            target.Transform.AttachToGridOrMap();
        }
    }
}
