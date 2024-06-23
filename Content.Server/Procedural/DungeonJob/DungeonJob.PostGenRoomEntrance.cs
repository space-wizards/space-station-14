using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Map;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="RoomEntranceDunGen"/>
    /// </summary>
    private async Task PostGen(RoomEntranceDunGen gen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        if (!data.Tiles.TryGetValue(DungeonDataKey.FallbackTile, out var tileProto) ||
            !data.SpawnGroups.TryGetValue(DungeonDataKey.Entrance, out var entranceProtos) ||
            !_prototype.TryIndex(entranceProtos, out var entranceIn))
        {
            LogDataError(typeof(RoomEntranceDunGen));
            return;
        }

        var setTiles = new List<(Vector2i, Tile)>();
        var tileDef = _tileDefManager[tileProto];

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                setTiles.Add((entrance, _tile.GetVariantTile((ContentTileDefinition) tileDef, random)));
            }
        }

        _maps.SetTiles(_gridUid, _grid, setTiles);

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                _entManager.SpawnEntities(
                    _maps.GridTileToLocal(_gridUid, _grid, entrance),
                    EntitySpawnCollection.GetSpawns(entranceIn.Entries, random));
            }
        }
    }
}
