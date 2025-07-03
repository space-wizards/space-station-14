using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;
using Robust.Shared.Map;
using Robust.Shared.Noise;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="SampleTileDunGen"/>
    /// </summary>
    private async Task PostGen(SampleTileDunGen gen,
        List<Dungeon> dungeons,
        HashSet<Vector2i> reservedTiles,
        Random random)
    {
        var noise = gen.Noise;
        var oldSeed = noise.GetSeed();
        noise.SetSeed(_seed + oldSeed);
        var tiles = new List<(Vector2i Index, Tile Tile)>();
        var tileDef = _prototype.Index(gen.Tile);
        var variants = tileDef.PlacementVariants.Length;

        foreach (var dungeon in dungeons)
        {
            foreach (var tile in dungeon.AllTiles)
            {
                if (reservedTiles.Contains(tile))
                    continue;

                var invert = gen.Invert;
                var value = noise.GetNoise(tile.X, tile.Y);
                value = invert ? value * -1 : value;

                if (value < gen.Threshold)
                    continue;

                var variantValue = (noise.GetNoise(tile.X * 8, tile.Y * 8, variants) + 1f) * 100;
                var variant = _tile.PickVariant(tileDef, (int)variantValue);
                var tileVariant = new Tile(tileDef.TileId, variant: variant);

                tiles.Add((tile, tileVariant));
                AddLoadedTile(tile, tileVariant);

                await SuspendDungeon();

                if (!ValidateResume())
                    return;
            }
        }

        gen.Noise.SetSeed(oldSeed);
        _maps.SetTiles(_gridUid, _grid, tiles);

        if (gen.ReserveTiles)
        {
            foreach (var tile in tiles)
            {
                reservedTiles.Add(tile.Index);
            }
        }
    }
}
