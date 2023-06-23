// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Shared.Administration;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.SS220.Commands;


[AnyCommand]
public sealed class OrbitCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public string Command => "orbit";
    public string Description => "You start following entity";

    public string Help => "orbit <entityUid>\nEntityUid to follow";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player as IPlayerSession;
        
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
        
        if (!EntityUid.TryParse(args[0], out var entityToFollow))
        {
            shell.WriteError($"{args[0]} is not a valid entity UID.");
            return;
        }

        if (player.Status != SessionStatus.InGame || player.AttachedEntity is not {Valid:true} playerEntity)
        {
            shell.WriteLine("You are not in-game!");
            return;
        }

        if (!_entityManager.HasComponent<GhostComponent>(playerEntity))
        {
            shell.WriteLine("You are not a ghost");
            return;
        }

        if (!_entityManager.HasComponent<MindComponent>(entityToFollow))
        {
            shell.WriteLine("You can't follow this entity");
            return;
        }
        
        _entityManager.System<FollowerSystem>().StartFollowingEntity(playerEntity, entityToFollow);
    }
}