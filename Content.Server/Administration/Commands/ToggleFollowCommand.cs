using Content.Shared.Administration;
using Content.Shared.Follower.Components;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ToggleFollowCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "togglefollow";
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

        if (!bool.TryParse(args[0], out var toggle))
        {
            shell.WriteLine(Loc.GetString("shell-argument-must-be-boolean"));
            return;
        }

        var hasComp = _entities.HasComponent<DenyFollowComponent>(player.AttachedEntity);
        switch (hasComp)
        {
            case true when !toggle: // If the entity has the component and the toggle is false, remove the component.
                _entities.RemoveComponent<DenyFollowComponent>(player.AttachedEntity.Value);
                break;
            case false when toggle: // If the entity doesn't have the component and the toggle is true, add the component.
                _entities.AddComponent<DenyFollowComponent>(player.AttachedEntity.Value);
                break;
        }
    }
}
