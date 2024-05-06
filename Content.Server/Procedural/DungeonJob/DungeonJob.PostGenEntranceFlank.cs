using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Collections;
using Robust.Shared.Map;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="EntranceFlankPostGen"/>
    /// </summary>
    private async Task PostGen(EntranceFlankPostGen gen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        var tiles = new List<(Vector2i Index, Tile)>();
        var tileDef = _tileDefManager[gen.Tile];
        var spawnPositions = new ValueList<Vector2i>(dungeon.Rooms.Count);

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                for (var i = 0; i < 8; i++)
                {
                    var dir = (Direction) i;
                    var neighbor = entrance + dir.ToIntVec();

                    if (!dungeon.RoomExteriorTiles.Contains(neighbor))
                        continue;

                    if (reservedTiles.Contains(neighbor))
                        continue;

                    tiles.Add((neighbor, _tile.GetVariantTile((ContentTileDefinition) tileDef, random)));
                    spawnPositions.Add(neighbor);
                }
            }
        }

        _maps.SetTiles(_gridUid, _grid, tiles);

        foreach (var entrance in spawnPositions)
        {
            _entManager.SpawnEntities(_maps.GridTileToLocal(_gridUid, _grid, entrance), gen.Entities);
        }
    }
}
