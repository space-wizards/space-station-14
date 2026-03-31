using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Follower;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class FollowCommand : LocalizedEntityCommands
{
    [Dependency] private readonly FollowerSystem _followerSystem = default!;

    public override string Command => "follow";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!CommandChecks.MustBeAttachedToEntity(shell, out _, out var entity) ||
            !CommandChecks.NeedExactlyOneArgument(shell, args))
            return;

        if (NetEntity.TryParse(args[0], out var uidNet) && EntityManager.TryGetEntity(uidNet, out var uid))
            _followerSystem.StartFollowingEntity(entity.Value, uid.Value);
    }
}
