using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Permissions)]
    public sealed class OpenPermissionsCommand : IConsoleCommand
    {
        public string Command => "permissions";
        public string Description => "Opens the admin permissions panel.";
        public string Help => "Usage: permissions";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("This does not work from the server console.");
                return;
            }

            var eui = IoCManager.Resolve<EuiManager>();
            var ui = new PermissionsEui();
            eui.OpenEui(ui, player);
        }
    }
}
