using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Collections;
using Robust.Shared.Map;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="EntranceFlankDunGen"/>
    /// </summary>
    private async Task PostGen(EntranceFlankDunGen gen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        if (!data.Tiles.TryGetValue(DungeonDataKey.FallbackTile, out var tileProto) ||
            !data.SpawnGroups.TryGetValue(DungeonDataKey.EntranceFlank, out var flankProto))
        {
            _sawmill.Error($"Unable to get dungeon data for {nameof(gen)}");
            return;
        }

        var tiles = new List<(Vector2i Index, Tile)>();
        var tileDef = _tileDefManager[tileProto];
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
        var entGroup = _prototype.Index(flankProto);

        foreach (var entrance in spawnPositions)
        {
            _entManager.SpawnEntities(_maps.GridTileToLocal(_gridUid, _grid, entrance), EntitySpawnCollection.GetSpawns(entGroup.Entries, random));
        }
    }
}
