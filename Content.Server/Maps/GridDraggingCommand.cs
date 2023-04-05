using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;

namespace Content.Server.Maps;

/// <summary>
/// Toggles GridDragging on the system.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class GridDraggingCommand : IConsoleCommand
{
    public string Command => SharedGridDraggingSystem.CommandName;
    public string Description => $"Allows someone with permissions to drag grids around.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteError("shell-server-cannot");
            return;
        }

        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GridDraggingSystem>();
        system.Toggle(shell.Player);

        if (system.IsEnabled(shell.Player))
            shell.WriteLine("Grid dragging toggled on");
        else
            shell.WriteLine("Grid dragging toggled off");
    }
}
