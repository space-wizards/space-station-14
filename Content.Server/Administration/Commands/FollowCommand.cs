using System.Linq;
using System.Numerics;
using Content.Server.Warps;
using Content.Shared.Administration;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class FollowCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "follow";
        public string Description => "Makes you begin following an entity.";

        public string Help =>
            "follow <netEntity>\nMakes you begin following the given entity. ";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("Only players can use this command");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine("Expected a single argument.");
                return;
            }

            if (player.Status != SessionStatus.InGame || player.AttachedEntity is not {Valid: true} playerEntity)
            {
                shell.WriteLine("You are not in-game!");
                return;
            }

            var entity = args[0];
            if (NetEntity.TryParse(entity, out var uidNet) && _entManager.TryGetEntity(uidNet, out var uid))
            {
                _entManager.System<FollowerSystem>().StartFollowingEntity(playerEntity, uid.Value);
            }
        }
    }
}
