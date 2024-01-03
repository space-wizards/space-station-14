using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ChangeVisibilityCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "changevisibility";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (player.AttachedEntity == null)
        {
            shell.WriteLine(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!int.TryParse(args[0], out var layer))
        {
            shell.WriteLine(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        if (!_entities.TryGetComponent<VisibilityComponent>(player.AttachedEntity, out var visibilityComponent))
        {
            shell.WriteLine(Loc.GetString("cmd-visibility-no-visibility-component"));
            return;
        }

        _entities.System<VisibilitySystem>().SetLayer((EntityUid) player.AttachedEntity, visibilityComponent, layer);
    }
}
