using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="FillGridDunGen"/>
    /// </summary>
    private async Task<Dungeon> GenerateFillDunGen(DungeonData data, HashSet<Vector2i> reservedTiles)
    {
        if (!data.Entities.TryGetValue(DungeonDataKey.Fill, out var fillEnt))
        {
            LogDataError(typeof(FillGridDunGen));
            return Dungeon.Empty;
        }

        var roomTiles = new HashSet<Vector2i>();
        var tiles = _maps.GetAllTilesEnumerator(_gridUid, _grid);

        while (tiles.MoveNext(out var tileRef))
        {
            var tile = tileRef.Value.GridIndices;

            if (reservedTiles.Contains(tile))
                continue;

            if (!_anchorable.TileFree(_grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            var gridPos = _maps.GridTileToLocal(_gridUid, _grid, tile);
            _entManager.SpawnEntity(fillEnt, gridPos);

            roomTiles.Add(tile);

            await SuspendDungeon();
            if (!ValidateResume())
                break;
        }

        var dungeon = new Dungeon();
        var room = new DungeonRoom(roomTiles, Vector2.Zero, Box2i.Empty, new HashSet<Vector2i>());
        dungeon.AddRoom(room);

        return dungeon;
    }
}
