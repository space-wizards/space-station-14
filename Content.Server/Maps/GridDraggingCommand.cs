using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;

namespace Content.Server.Maps;

/// <summary>
/// Toggles GridDragging on the system.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class GridDraggingCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GridDraggingSystem _grid = default!;

    public override string Command => SharedGridDraggingSystem.CommandName;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteError("shell-only-players-can-run-this-command");
            return;
        }

        _grid.Toggle(shell.Player);
        shell.WriteLine(Loc.GetString($"cmd-griddrag-status", ("status", _grid.IsEnabled(shell.Player))));
    }
}
