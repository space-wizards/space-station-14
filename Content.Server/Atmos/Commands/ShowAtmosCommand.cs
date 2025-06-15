using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class ShowAtmos : LocalizedEntityCommands
    {
        [Dependency] private readonly AtmosDebugOverlaySystem _atmosDebug = default!;

        public override string Command => "showatmos";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString($"shell-only-players-can-run-this-command"));
                return;
            }

            var enabled = _atmosDebug.ToggleObserver(player);

            shell.WriteLine(Loc.GetString($"cmd-showatmos-status", ("status", enabled)));
        }
    }
}
