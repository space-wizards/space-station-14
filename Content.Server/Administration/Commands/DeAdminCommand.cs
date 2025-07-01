using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.None)]
    public sealed class DeAdminCommand : IConsoleCommand
    {
        public string Command => "deadmin";
        public string Description => "Temporarily de-admins you so you can experience the round as a normal player.";
        public string Help => "Usage: deadmin\nUse readmin to re-admin after using this.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("You cannot use this command from the server console.");
                return;
            }

            var mgr = IoCManager.Resolve<IAdminManager>();
            mgr.DeAdmin(player);
        }
    }
}
