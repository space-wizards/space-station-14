using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class ShowAtmos : LocalizedEntityCommands
    {
        [Dependency] private readonly AtmosDebugOverlaySystem _atmosDebugOverlaySystem = default!;

        public override string Command => "showatmos";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            var atmosDebug = _atmosDebugOverlaySystem;
            var enabled = atmosDebug.ToggleObserver(player);

            shell.WriteLine(enabled
                ? Loc.GetString("cmd-showatmos-enabled")
                : Loc.GetString("cmd-showatmos-disabled"));
        }
    }
}
