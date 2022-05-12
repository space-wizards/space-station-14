using Content.Shared.Maps;
using Robust.Shared.Console;

namespace Content.Client.Maps;

/// <summary>
/// Toggles GridDragging on the system.
/// </summary>
public sealed class GridDraggingCommand : IConsoleCommand
{
    public string Command => SharedGridDraggingSystem.CommandName;
    public string Description => $"Allows someone with permissions to drag grids around.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GridDraggingSystem>().Enabled ^= true;
    }
}
