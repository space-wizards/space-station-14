using System.Text;
using Content.Server.Administration;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Mobs
{
    [AdminCommand(AdminFlags.Admin)]
    public class MindInfoCommand : IClientCommand
    {
        public string Command => "mindinfo";

        public string Description => "Lists info for the mind of a specific player.";

        public string Help => "mindinfo <session ID>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Expected exactly 1 argument.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetSessionByUsername(args[0], out var data))
            {
                var mind = data.ContentData().Mind;

                var builder = new StringBuilder();
                builder.AppendFormat("player: {0}, mob: {1}\nroles: ", mind.UserId, mind.OwnedMob?.Owner?.Uid);
                foreach (var role in mind.AllRoles)
                {
                    builder.AppendFormat("{0} ", role.Name);
                }

                shell.SendText(player, builder.ToString());
            }
            else
            {
                shell.SendText(player, "Can't find that mind");
            }
        }
    }
}