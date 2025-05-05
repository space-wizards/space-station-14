using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;

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
        gen.Noise.SetSeed(random.Next());

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

                AddLoadedTile(tile, gen.Tile);
            }
        }
    }
}
