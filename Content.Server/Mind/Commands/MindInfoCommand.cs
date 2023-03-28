using System.Text;
using Content.Server.Administration;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Mind.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class MindInfoCommand : IConsoleCommand
    {
        public string Command => "mindinfo";

        public string Description => "Lists info for the mind of a specific player.";

        public string Help => "mindinfo <session ID>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Expected exactly 1 argument.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (!mgr.TryGetSessionByUsername(args[0], out var data))
            {
                shell.WriteLine("Can't find that mind");
                return;
            }

            var mind = data.ContentData()?.Mind;

            if (mind == null)
            {
                shell.WriteLine("Can't find that mind");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendFormat("player: {0}, mob: {1}\nroles: ", mind.UserId, mind.OwnedComponent?.Owner);
            foreach (var role in mind.AllRoles)
            {
                builder.AppendFormat("{0} ", role.Name);
            }

            shell.WriteLine(builder.ToString());
        }
    }
}
