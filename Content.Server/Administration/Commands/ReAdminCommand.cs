using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

#nullable enable

namespace Content.Server.Administration.Commands
{
    [AnyCommand]
    public class ReAdminCommand : IClientCommand
    {
        public string Command => "readmin";
        public string Description => "Re-admins you if you previously de-adminned.";
        public string Help => "Usage: readmin";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "You cannot use this command from the server console.");
                return;
            }

            var mgr = IoCManager.Resolve<IAdminManager>();

            if (mgr.GetAdminData(player, includeDeAdmin: true) == null)
            {
                shell.SendText(player, "You're not an admin.");
                return;
            }

            mgr.ReAdmin(player);
        }
    }
}
