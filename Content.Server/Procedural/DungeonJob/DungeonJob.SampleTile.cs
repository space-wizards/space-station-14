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
        var oldSeed = gen.Noise.GetSeed();
        gen.Noise.SetSeed(_seed + oldSeed);
        var tiles = new List<(Vector2i Index, Tile Tile)>();
        var tileDef = _prototype.Index(gen.Tile);

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

                tiles.Add((tile, new Tile(tileDef.TileId)));
            }
        }

        gen.Noise.SetSeed(oldSeed);
        _maps.SetTiles(_gridUid, _grid, tiles);

        foreach (var tile in tiles)
        {
            if (gen.ReserveTiles)
            {
                reservedTiles.Add(tile.Index);
            }

            AddLoadedTile(tile.Index, tile.Tile);
        }
    }
}
