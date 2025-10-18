using System.Linq;
using System.Numerics;
using Content.Shared.Administration;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Content.Shared.Warps;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class WarpCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "warp";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("shell-need-exactly-one-argument"));
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
                if (player.Status != SessionStatus.InGame || player.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteLine(Loc.GetString("cmd-warp-not-in-game"));
                    return;
                }

                var currentMap = _entManager.GetComponent<TransformComponent>(playerEntity).MapID;
                var currentGrid = _entManager.GetComponent<TransformComponent>(playerEntity).GridUid;

                var xformSystem = _entManager.System<SharedTransformSystem>();

                var found = GetWarpPointByName(location)
                    .OrderBy(p => p.Item1, Comparer<EntityCoordinates>.Create((a, b) =>
                    {
                        // Sort so that warp points on the same grid/map are first.
                        // So if you have two maps loaded with the same warp points,
                        // it will prefer the warp points on the map you're currently on.
                        var aGrid = xformSystem.GetGrid(a);
                        var bGrid = xformSystem.GetGrid(b);

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

                        var mapA = xformSystem.GetMapId(a);
                        var mapB = xformSystem.GetMapId(b);

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
                    shell.WriteError(Loc.GetString("cmd-warp-location-not-found"));
                    return;
                }

                if (follow && _entManager.HasComponent<GhostComponent>(playerEntity))
                {
                    _entManager.System<FollowerSystem>().StartFollowingEntity(playerEntity, coords.EntityId);
                    return;
                }

                xformSystem.SetCoordinates(playerEntity, coords);
                xformSystem.AttachToGridOrMap(playerEntity);
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

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = new[] { "?" }.Concat(GetWarpPointNames());

                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-warp-completion-hint"));
            }

            return CompletionResult.Empty;
        }
    }
}
