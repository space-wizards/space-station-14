using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class AnnounceUiCommand : IConsoleCommand
    {
        public string Command => "announceui";

        public string Description => "Opens the announcement UI";

        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("This does not work from the server console.");
                return;
            }

            var eui = IoCManager.Resolve<EuiManager>();
            var ui = new AdminAnnounceEui();
            eui.OpenEui(ui, player);
        }
    }
}
