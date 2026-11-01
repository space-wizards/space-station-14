using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Mapping;

[AdminCommand(AdminFlags.Debug)]
public sealed partial class ShowRoofs : IConsoleCommand
{
    [Dependency] private IEntityManager _e = default!;

    public string Command => "showroofs";
    public string Description => "Toggles seeing the roof debug overlay.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("You must be a player to use this command.");
            return;
        }

        var atmosDebug = _e.System<ServerRoofDebugOverlaySystem>();
        var enabled = atmosDebug.ToggleObserver(player);

        shell.WriteLine(enabled
            ? "Enabled the roof debug overlay."
            : "Disabled the roof debug overlay.");
    }
}

