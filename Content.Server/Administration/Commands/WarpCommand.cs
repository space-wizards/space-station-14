using System.Linq;
using Content.Server.Ghost.Components;
using Content.Server.Warps;
using Content.Shared.Administration;
using Content.Shared.Follower;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class WarpCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "warp";
        public string Description => "Teleports you to predefined areas on the map.";

        public string Help =>
            "warp <location>\nLocations you can teleport to are predefined by the map. " +
            "You can specify '?' as location to get a list of valid locations.";

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

            var location = args[0];
            if (location == "?")
            {
                var locations = string.Join(", ", GetWarpPointNames());

                shell.WriteLine(locations);
            }
            else
            {
                if (player.Status != SessionStatus.InGame || player.AttachedEntity is not {Valid: true} playerEntity)
                {
                    shell.WriteLine("You are not in-game!");
                    return;
                }

                var currentMap = _entManager.GetComponent<TransformComponent>(playerEntity).MapID;
                var currentGrid = _entManager.GetComponent<TransformComponent>(playerEntity).GridUid;

                var found = _entManager.EntityQuery<WarpPointComponent>(true)
                    .Where(p => p.Location == location)
                    .Select(p => (_entManager.GetComponent<TransformComponent>(p.Owner).Coordinates, p.Follow))
                    .OrderBy(p => p.Item1, Comparer<EntityCoordinates>.Create((a, b) =>
                    {
                        // Sort so that warp points on the same grid/map are first.
                        // So if you have two maps loaded with the same warp points,
                        // it will prefer the warp points on the map you're currently on.
                        var aGrid = a.GetGridUid(_entManager);
                        var bGrid = b.GetGridUid(_entManager);

                        if (aGrid == bGrid)
                        {
                            return 0;
                        }

                        if (aGrid == currentGrid)
                        {
                            return -1;
                        }

                        if (bGrid == currentGrid)
                        {
                            return 1;
                        }

                        var mapA = a.GetMapId(_entManager);
                        var mapB = a.GetMapId(_entManager);

                        if (mapA == mapB)
                        {
                            return 0;
                        }

                        if (mapA == currentMap)
                        {
                            return -1;
                        }

                        if (mapB == currentMap)
                        {
                            return 1;
                        }

                        return 0;
                    }))
                    .FirstOrDefault();

                var (coords, follow) = found;

                if (coords.EntityId == EntityUid.Invalid)
                {
                    shell.WriteError("That location does not exist!");
                    return;
                }

                if (follow && _entManager.HasComponent<GhostComponent>(playerEntity))
                {
                    _entManager.System<FollowerSystem>().StartFollowingEntity(playerEntity, coords.EntityId);
                    return;
                }

                var xform = _entManager.GetComponent<TransformComponent>(playerEntity);
                xform.Coordinates = coords;
                xform.AttachToGridOrMap();
                if (_entManager.TryGetComponent(playerEntity, out PhysicsComponent? physics))
                {
                    _entManager.System<SharedPhysicsSystem>().SetLinearVelocity(playerEntity, Vector2.Zero, body: physics);
                }
            }
        }

        private IEnumerable<string> GetWarpPointNames()
        {
            return _entManager.EntityQuery<WarpPointComponent>(true)
                .Select(p => p.Location)
                .Where(p => p != null)
                .OrderBy(p => p)
                .Distinct()!;
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = new[] { "?" }.Concat(GetWarpPointNames());

                return CompletionResult.FromHintOptions(options, "<warp point | ?>");
            }

            return CompletionResult.Empty;
        }
    }
}
