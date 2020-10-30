using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

#nullable enable

namespace Content.Server.Administration
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.None)]
    public class DeAdminCommand : IClientCommand
    {
        public string Command => "deadmin";
        public string Description => "Temporarily de-admins you so you can experience the round as a normal player.";
        public string Help => "Usage: deadmin\nUse readmin to re-admin after using this.";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "You cannot use this command from the server console.");
                return;
            }

            var mgr = IoCManager.Resolve<IAdminManager>();
            mgr.DeAdmin(player);
        }
    }
}
