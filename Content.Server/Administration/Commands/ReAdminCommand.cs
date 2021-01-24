using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

#nullable enable

namespace Content.Server.Administration.Commands
{
    [AnyCommand]
    public class ReAdminCommand : IServerCommand
    {
        public string Command => "readmin";
        public string Description => "Re-admins you if you previously de-adminned.";
        public string Help => "Usage: readmin";

        public void Execute(IServerConsoleShell shell, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You cannot use this command from the server console.");
                return;
            }

            var mgr = IoCManager.Resolve<IAdminManager>();

            if (mgr.GetAdminData(player, includeDeAdmin: true) == null)
            {
                shell.WriteLine("You're not an admin.");
                return;
            }

            mgr.ReAdmin(player);
        }
    }
}
