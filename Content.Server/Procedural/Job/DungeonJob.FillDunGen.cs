using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Collections;

namespace Content.Server.Procedural.Job;

public sealed partial class DungeonJob
{
    private async Task<ValueList<Dungeon>> GenerateFillDungeon(FillGridDunGen dungen, HashSet<Vector2i> reservedTiles)
    {
        var tiles = _maps.GetAllTilesEnumerator(_gridUid, _grid);

        while (tiles.MoveNext(out var tileRef))
        {
            var tile = tileRef.Value.GridIndices;

            if (reservedTiles.Contains(tile))
                continue;

            if (!_anchorable.TileFree(_grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            var gridPos = _maps.GridTileToLocal(_gridUid, _grid, tile);
            _entManager.SpawnEntity(dungen.Proto, gridPos);
            await SuspendDungeon();
            if (!ValidateResume())
                break;
        }

        return new ValueList<Dungeon>();
    }
}
