using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="DungeonEntranceDunGen"/>
    /// </summary>
    private async Task PostGen(DungeonEntranceDunGen gen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        var rooms = new List<DungeonRoom>(dungeon.Rooms);
        var roomTiles = new List<Vector2i>();
        var tileDef = (ContentTileDefinition) _tileDefManager[gen.Tile];
        var contents = _prototype.Index(gen.Contents);

        for (var i = 0; i < gen.Count; i++)
        {
            var roomIndex = random.Next(rooms.Count);
            var room = rooms[roomIndex];

            // Move out 3 tiles in a direction away from center of the room
            // If none of those intersect another tile it's probably external
            // TODO: Maybe need to take top half of furthest rooms in case there's interior exits?
            roomTiles.AddRange(room.Exterior);
            random.Shuffle(roomTiles);

            foreach (var tile in roomTiles)
            {
                var isValid = false;

                // Check if one side is dungeon and the other side is nothing.
                for (var j = 0; j < 4; j++)
                {
                    var dir = (Direction) (j * 2);
                    var oppositeDir = dir.GetOpposite();
                    var dirVec = tile + dir.ToIntVec();
                    var oppositeDirVec = tile + oppositeDir.ToIntVec();

                    if (!dungeon.RoomTiles.Contains(dirVec))
                    {
                        continue;
                    }

                    if (dungeon.RoomTiles.Contains(oppositeDirVec) ||
                        dungeon.RoomExteriorTiles.Contains(oppositeDirVec) ||
                        dungeon.CorridorExteriorTiles.Contains(oppositeDirVec) ||
                        dungeon.CorridorTiles.Contains(oppositeDirVec))
                    {
                        continue;
                    }

                    // Check if exterior spot free.
                    if (!_anchorable.TileFree(_grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    {
                        continue;
                    }

                    // Check if interior spot free (no guarantees on exterior but ClearDoor should handle it)
                    if (!_anchorable.TileFree(_grid, dirVec, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    {
                        continue;
                    }

                    // Valid pick!
                    isValid = true;

                    // Entrance wew
                    _maps.SetTile(_gridUid, _grid, tile, _tile.GetVariantTile(tileDef, random));
                    ClearDoor(dungeon, _grid, tile);
                    var gridCoords = _maps.GridTileToLocal(_gridUid, _grid, tile);
                    // Need to offset the spawn to avoid spawning in the room.

                    foreach (var ent in _entTable.GetSpawns(contents, random))
                    {
                        _entManager.SpawnAtPosition(ent, gridCoords);
                    }

                    // Clear out any biome tiles nearby to avoid blocking it
                    foreach (var nearTile in _maps.GetLocalTilesIntersecting(_gridUid, _grid, new Circle(gridCoords.Position, 1.5f), false))
                    {
                        if (dungeon.RoomTiles.Contains(nearTile.GridIndices) ||
                            dungeon.RoomExteriorTiles.Contains(nearTile.GridIndices) ||
                            dungeon.CorridorTiles.Contains(nearTile.GridIndices) ||
                            dungeon.CorridorExteriorTiles.Contains(nearTile.GridIndices))
                        {
                            continue;
                        }

                        _maps.SetTile(_gridUid, _grid, nearTile.GridIndices, _tile.GetVariantTile(tileDef, random));
                    }

                    break;
                }

                if (isValid)
                    break;
            }

            roomTiles.Clear();
        }
    }
}
