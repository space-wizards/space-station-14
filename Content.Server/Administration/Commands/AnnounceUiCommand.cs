using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class AnnounceUiCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly EuiManager _euiManager = default!;

        public override string Command => "announceui";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString($"shell-cannot-run-command-from-server"));
                return;
            }

            var ui = new AdminAnnounceEui();
            _euiManager.OpenEui(ui, player);
        }
    }
}
