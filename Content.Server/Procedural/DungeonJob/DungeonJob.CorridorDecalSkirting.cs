using System.Threading.Tasks;
using Content.Shared.Doors.Components;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Collections;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="CorridorDecalSkirtingDunGen"/>
    /// </summary>
    private async Task PostGen(CorridorDecalSkirtingDunGen decks, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        var directions = new ValueList<DirectionFlag>(4);
        var pocketDirections = new ValueList<Direction>(4);
        var doorQuery = _entManager.GetEntityQuery<DoorComponent>();
        var physicsQuery = _entManager.GetEntityQuery<PhysicsComponent>();
        var offset = -_grid.TileSizeHalfVector;

        foreach (var tile in dungeon.CorridorTiles)
        {
            DebugTools.Assert(!dungeon.RoomTiles.Contains(tile));
            directions.Clear();

            // Do cardinals 1 step
            // Do corners the other step
            for (var i = 0; i < 4; i++)
            {
                var dir = (DirectionFlag) Math.Pow(2, i);
                var neighbor = tile + dir.AsDir().ToIntVec();

                var anc = _maps.GetAnchoredEntitiesEnumerator(_gridUid, _grid, neighbor);

                while (anc.MoveNext(out var ent))
                {
                    if (!physicsQuery.TryGetComponent(ent, out var physics) ||
                        !physics.CanCollide ||
                        !physics.Hard ||
                        doorQuery.HasComponent(ent.Value))
                    {
                        continue;
                    }

                    directions.Add(dir);
                    break;
                }
            }

            // Pockets
            if (directions.Count == 0)
            {
                pocketDirections.Clear();

                for (var i = 1; i < 5; i++)
                {
                    var dir = (Direction) (i * 2 - 1);
                    var neighbor = tile + dir.ToIntVec();

                    var anc = _maps.GetAnchoredEntitiesEnumerator(_gridUid, _grid, neighbor);

                    while (anc.MoveNext(out var ent))
                    {
                        if (!physicsQuery.TryGetComponent(ent, out var physics) ||
                            !physics.CanCollide ||
                            !physics.Hard ||
                            doorQuery.HasComponent(ent.Value))
                        {
                            continue;
                        }

                        pocketDirections.Add(dir);
                        break;
                    }
                }

                if (pocketDirections.Count == 1)
                {
                    if (decks.PocketDecals.TryGetValue(pocketDirections[0], out var cDir))
                    {
                        // Decals not being centered biting my ass again
                        var gridPos = _maps.GridTileToLocal(_gridUid, _grid, tile).Offset(offset);
                        _decals.TryAddDecal(cDir, gridPos, out _, color: decks.Color);
                    }
                }

                continue;
            }

            if (directions.Count == 1)
            {
                if (decks.CardinalDecals.TryGetValue(directions[0], out var cDir))
                {
                    // Decals not being centered biting my ass again
                    var gridPos = _maps.GridTileToLocal(_gridUid, _grid, tile).Offset(offset);
                    _decals.TryAddDecal(cDir, gridPos, out _, color: decks.Color);
                }

                continue;
            }

            // Corners
            if (directions.Count == 2)
            {
                // Auehghegueugegegeheh help me
                var dirFlag = directions[0] | directions[1];

                if (decks.CornerDecals.TryGetValue(dirFlag, out var cDir))
                {
                    var gridPos = _maps.GridTileToLocal(_gridUid, _grid, tile).Offset(offset);
                    _decals.TryAddDecal(cDir, gridPos, out _, color: decks.Color);
                }
            }
        }
    }
}
