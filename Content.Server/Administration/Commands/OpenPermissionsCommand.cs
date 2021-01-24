using Content.Server.Eui;
using Content.Shared.Administration;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

#nullable enable

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Permissions)]
    public sealed class OpenPermissionsCommand : IServerCommand
    {
        public string Command => "permissions";
        public string Description => "Opens the admin permissions panel.";
        public string Help => "Usage: permissions";

        public void Execute(IServerConsoleShell shell, string[] args)
        {
            var player = shell.Player as IPlayerSession;
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
