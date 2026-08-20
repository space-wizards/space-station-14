using Robust.Shared.Console;

namespace Content.Client.Ghost.Commands;

public sealed partial class ToggleGhostVisibilityCommand : LocalizedEntityCommands
{
    [Dependency] private GhostSystem _ghost = default!;

    public override string Command => "toggleghostvisibility";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _ghost.ToggleGhostVisibility();
    }
}
