using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Doors.Components;
using Robust.Shared.Collections;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Server.Mapping;

[AdminCommand(AdminFlags.Mapping)]
public sealed class FirelockifyCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _entManager = default!;

    public override string Command => "firelockify";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
        {
            return CompletionResult.Empty;
        }

        return CompletionResult.FromOptions(CompletionHelper.Components<MapGridComponent>(args[0], _entManager));
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid) ||
            !_entManager.TryGetComponent<MapGridComponent>(uid, out var grid))
        {
            return;
        }

        var tileEnumerator = grid.GetAllTilesEnumerator();
        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var conversions = new HashSet<string>()
        {
            "FirelockGlass"
        };

        var firelockQuery = _entManager.GetEntityQuery<FirelockComponent>();
        var physicsQuery = _entManager.GetEntityQuery<PhysicsComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var deleted = new ValueList<EntityUid>();
        var spawns = new ValueList<(Vector2i, Angle)>();
        var sawmill = Logger.GetSawmill("firelockify");
        var errorCount = 0;

        while (tileEnumerator.MoveNext(out var tile))
        {
            var anchored = grid.GetAnchoredEntitiesEnumerator(tile.Value.GridIndices);
            var validTile =false;

            while (anchored.MoveNext(out var ancUid))
            {
                if (!metaQuery.TryGetComponent(ancUid, out var meta))
                    continue;

                var proto = meta.EntityPrototype?.ID;

                if (string.IsNullOrEmpty(proto) || !conversions.Contains(proto))
                    continue;

                deleted.Add(ancUid.Value);
                validTile = true;
            }

            if (!validTile)
                continue;

            // Check neighbors
            // We will make 2 edge firelocks perpendicular to either:
            // 2 firelock neighbors, or
            // 1 wall neighbor
            var direction = DirectionFlag.None;

            for (var i = 0; i < 4; i++)
            {
                var neighborDir = (DirectionFlag) Math.Pow(2, i);
                var neighbor = tile.Value.GridIndices + neighborDir.AsDir().ToIntVec();

                anchored = grid.GetAnchoredEntitiesEnumerator(neighbor);

                while (anchored.MoveNext(out var neighborUid))
                {
                    if ((!physicsQuery.TryGetComponent(neighborUid, out var physics) ||
                         !physics.CanCollide) &&
                        !firelockQuery.HasComponent(neighborUid.Value))
                    {
                        continue;
                    }

                    // Get the next dir
                    direction = (DirectionFlag) ((int) neighborDir * 2 % 16);
                    break;
                }

                if (direction == DirectionFlag.None)
                    continue;

                if (direction == DirectionFlag.North)
                    direction = DirectionFlag.South;

                if (direction == DirectionFlag.West)
                    direction = DirectionFlag.East;

                spawns.Add((tile.Value.GridIndices, direction.AsDir().ToAngle()));

                break;
            }

            if (direction == DirectionFlag.None)
            {
                sawmill.Error($"Error converting tile at {tile.Value.GridIndices}");
                errorCount++;
            }
        }

        if (errorCount > 0)
        {
            sawmill.Error($"Error converting {errorCount} tiles.");
        }

        foreach (var ancUid in deleted)
        {
            _entManager.DeleteEntity(ancUid);
        }

        var xformSystem = _entManager.System<SharedTransformSystem>();

        foreach (var (tile, angle) in spawns)
        {
            var coords = grid.GridTileToLocal(tile);
            var angle1 = angle;
            var angle2 = angle + Math.PI;

            var firelock1 = _entManager.CreateEntityUninitialized("FirelockEdge", coords);
            var firelock2 = _entManager.CreateEntityUninitialized("FirelockEdge", coords);

            var xform1 = xformQuery.GetComponent(firelock1);
            var xform2 = xformQuery.GetComponent(firelock2);

            xformSystem.SetLocalRotation(xform1, angle1);
            xformSystem.SetLocalRotation(xform2, angle2);

            _entManager.InitializeAndStartEntity(firelock1);
            _entManager.InitializeAndStartEntity(firelock2);
            DebugTools.Assert(xform1.Anchored);
            DebugTools.Assert(xform2.Anchored);
        }
    }
}
