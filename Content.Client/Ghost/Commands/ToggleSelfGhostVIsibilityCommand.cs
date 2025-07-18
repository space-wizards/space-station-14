using Robust.Shared.Console;

namespace Content.Client.Ghost.Commands;

public sealed class ToggleSelfGhostVisibilityCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GhostSystem _ghost = default!;

    public override string Command => "toggleselfghostvisibility";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0 && bool.TryParse(args[0], out var visibility))
            _ghost.ToggleSelfGhostVisibility(visibility);
        else
            _ghost.ToggleSelfGhostVisibility();
    }
}
