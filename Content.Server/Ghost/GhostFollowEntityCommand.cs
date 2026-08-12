using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Ghost;

[AnyCommand]
internal sealed partial class GhostFollowEntityCommand : LocalizedEntityCommands
{
    public const string CommandName = "ghost_follow_entity";

    [Dependency] private GhostSystem _ghost = null!;

    public override string Command => CommandName;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || shell.Player is not { } player)
            return;

        var target = args[0];
        if (!NetEntity.TryParse(target, out var targetEnt))
            return;

        _ghost.GhostWarpRequest(player, targetEnt);
    }
}
