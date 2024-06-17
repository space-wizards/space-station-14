using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="FillGridDunGen"/>
    /// </summary>
    private async Task GenerateFillDungeon(Vector2i position,
        DungeonData data,
        FillGridDunGen dungen,
        HashSet<Vector2i> reservedTiles,
        int seed)
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
    }
}
