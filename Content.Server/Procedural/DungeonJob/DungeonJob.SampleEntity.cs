using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="SampleEntityDunGen"/>
    /// </summary>
    private async Task PostGen(
        SampleEntityDunGen gen,
        List<Dungeon> dungeons,
        HashSet<Vector2i> reservedTiles,
        Random random)
    {
        var oldSeed = gen.Noise.GetSeed();
        gen.Noise.SetSeed(_seed + oldSeed);

        foreach (var dungeon in dungeons)
        {
            foreach (var tile in dungeon.AllTiles)
            {
                if (reservedTiles.Contains(tile))
                    continue;

                var invert = gen.Invert;
                var value = gen.Noise.GetNoise(tile.X, tile.Y);
                value = invert ? value * -1 : value;

                if (value < gen.Threshold)
                    continue;

                // Not allowed
                if (!_maps.TryGetTileRef(_gridUid, _grid, tile, out var tileRef) ||
                    !gen.AllowedTiles.Contains(_tileDefManager[tileRef.Tile.TypeId].ID))
                {
                    continue;
                }

                var gridTile = _maps.GridTileToLocal(_gridUid, _grid, tile);
                var uid = _entManager.SpawnAttachedTo(random.Pick(gen.Entities), gridTile);
                AddLoadedEntity(tile, uid);

                if (gen.ReserveTiles)
                {
                    reservedTiles.Add(tile);
                }

                await SuspendDungeon();

                if (!ValidateResume())
                    return;
            }
        }

        gen.Noise.SetSeed(oldSeed);
    }
}
