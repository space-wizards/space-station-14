using System.Threading.Tasks;
using Content.Server.Light.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    public async Task RoofGen(RoofDunGen roof, List<Dungeon> dungeons, HashSet<Vector2i> reservedTiles, Random random)
    {
        var roofComp = _entManager.EnsureComponent<RoofComponent>(_gridUid);

        var noise = roof.Noise;
        var oldSeed = noise?.GetSeed() ?? 0;
        noise?.SetSeed(_seed + oldSeed);
        var rooves = _entManager.System<RoofSystem>();

        foreach (var dungeon in dungeons)
        {
            foreach (var tile in dungeon.AllTiles)
            {
                if (reservedTiles.Contains(tile))
                    continue;

                var value = noise?.GetNoise(tile.X, tile.Y) ?? 1f;

                if (value < roof.Threshold)
                    continue;

                rooves.SetRoof((_gridUid, _grid, roofComp), tile, true);
            }
        }

        noise?.SetSeed(oldSeed);
    }
}
