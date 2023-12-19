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

                var found = GetWarpPointByName(location)
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
            List<string> points = new(_entManager.Count<WarpPointComponent>());
            var query = _entManager.AllEntityQueryEnumerator<WarpPointComponent, MetaDataComponent>();
            while (query.MoveNext(out _, out var warp, out var meta))
            {
                points.Add(warp.Location ?? meta.EntityName);
            }

            points.Sort();
            return points;
        }

        private List<(EntityCoordinates, bool)> GetWarpPointByName(string name)
        {
            List<(EntityCoordinates, bool)> points = new();
            var query = _entManager.AllEntityQueryEnumerator<WarpPointComponent, MetaDataComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var warp, out var meta, out var xform))
            {
                if (name == (warp.Location ?? meta.EntityName))
                    points.Add((xform.Coordinates, warp.Follow));
            }

            return points;
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
