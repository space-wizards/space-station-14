using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Mind.Verbs
{
    [GlobalVerb]
    public class MakeSentientVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            var groupController = IoCManager.Resolve<IConGroupController>();

            if (user == target || target.HasComponent<MindComponent>())
                return;

            var player = user.GetComponent<ActorComponent>().PlayerSession;
            if (groupController.CanCommand(player, "makesentient"))
            {
                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Make Sentient");
                data.CategoryData = VerbCategories.Debug;
                data.IconTexture = "/Textures/Interface/VerbIcons/sentient.svg.192dpi.png";
            }
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();

            var player = user.GetComponent<ActorComponent>().PlayerSession;
            if (!groupController.CanCommand(player, "makesentient"))
                return;

            var host = IoCManager.Resolve<IServerConsoleHost>();
            var cmd = new MakeSentientCommand();
            var uidStr = target.Uid.ToString();
            cmd.Execute(new ConsoleShell(host, player), $"{cmd.Command} {uidStr}",
                new[] {uidStr});
        }
    }
}
