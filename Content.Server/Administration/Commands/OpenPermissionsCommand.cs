using Content.Server.Eui;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

#nullable enable

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Permissions)]
    public sealed class OpenPermissionsCommand : IClientCommand
    {
        public string Command => "permissions";
        public string Description => "Opens the admin permissions panel.";
        public string Help => "Usage: permissions";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "This does not work from the server console.");
                return;
            }

            var eui = IoCManager.Resolve<EuiManager>();
            var ui = new PermissionsEui();
            eui.OpenEui(ui, player);
        }
    }
}
