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

        public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-must-be-player"));
                return;
            }

            var atmosDebug = _atmosDebugOverlaySystem;
            var enabled = atmosDebug.ToggleObserver(player);

            shell.WriteLine(enabled
                ? Loc.GetString($"cmd-{Command}-enabled")
                : Loc.GetString($"cmd-{Command}-disabled"));
        }
    }
}
