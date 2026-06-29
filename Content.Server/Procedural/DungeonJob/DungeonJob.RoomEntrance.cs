using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="RoomEntranceDunGen"/>
    /// </summary>
    private async Task PostGen(RoomEntranceDunGen gen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, IRobustRandom random)
    {
        var setTiles = new List<(Vector2i, Tile)>();
        var tileDef = _tileDefManager[gen.Tile];

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                if (reservedTiles.Contains(entrance))
                    continue;

                setTiles.Add((entrance, _tile.GetVariantTile((ContentTileDefinition) tileDef, random)));
            }
        }

        _maps.SetTiles(_gridUid, _grid, setTiles);

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                if (reservedTiles.Contains(entrance))
                    continue;

                _entManager.SpawnEntitiesAttachedTo(
                    _maps.GridTileToLocal(_gridUid, _grid, entrance),
                    _entTable.GetSpawns(gen.Contents, random));

                await SuspendDungeon();

                if (!ValidateResume())
                    return;
            }
        }
    }
}
