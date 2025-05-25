using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.PostGeneration;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="BiomeDunGen"/>
    /// </summary>
    private async Task<Dungeon> PostGen(ChunkDunGen dunGen, HashSet<Vector2i> reservedTiles, Random random)
    {
        var dungeon = new Dungeon();
        var tiles = new HashSet<Vector2i>();
        var tr = _position + new Vector2i(dunGen.Size, dunGen.Size);
        var oldSeed = dunGen.Noise?.GetSeed() ?? 0;
        dunGen.Noise?.SetSeed(_seed + oldSeed);

        for (var x = 0; x < dunGen.Size; x++)
        {
            for (var y = 0; y < dunGen.Size; y++)
            {
                var index = new Vector2i(_position.X + x, _position.Y + y);

                if (reservedTiles.Contains(index))
                    continue;

                if (dunGen.Noise?.GetNoise(x, y) < dunGen.Threshold)
                    continue;

                tiles.Add(index);
            }
        }

        dunGen.Noise?.SetSeed(oldSeed);
        var room = new DungeonRoom(tiles, (tr - _position) / 2 + _position, new Box2i(_position, tr), new HashSet<Vector2i>());
        dungeon.AddRoom(room);
        return dungeon;
    }
}
