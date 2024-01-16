using Content.Shared.Administration;
using Content.Shared.Follower;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class FollowCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "follow";
    public string Description => Loc.GetString("add-uplink-command-description");
    public string Help => Loc.GetString("add-uplink-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (player.Status != SessionStatus.InGame || player.AttachedEntity is not { Valid: true } playerEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        var entity = args[0];
        if (NetEntity.TryParse(entity, out var uidNet) && _entManager.TryGetEntity(uidNet, out var uid))
        {
            _entManager.System<FollowerSystem>().StartFollowingEntity(playerEntity, uid.Value);
        }
    }
}
