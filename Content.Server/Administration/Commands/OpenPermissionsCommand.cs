using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Permissions)]
    public sealed class OpenPermissionsCommand : LocalizedCommands
    {
        [Dependency] private readonly EuiManager _eui = default!;

        public override string Command => "permissions";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            var ui = new PermissionsEui();
            _eui.OpenEui(ui, player);
        }
    }
}
