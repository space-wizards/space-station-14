using System.Security.Cryptography;
using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Procedural;

public sealed partial class DungeonJob
{
    /// <summary>
    /// Generates corridors first via worms then places rooms on ends.
    /// </summary>
    private async Task<Dungeon> GenerateWormDungeon(WormDunGen dungen, EntityUid gridUid, MapGridComponent grid,
        int seed)
    {
        var random = new Random(seed);
        var dungeon = new Dungeon();
        var tiles = new List<(Vector2i Index, Tile Tile)>();
        // TODO: Share code with prefabdungens
        var position = _position;
        var tileDef = _prototype.Index(dungen.Tile);
        tiles.Add((position, new Tile(tileDef.TileId, variant: random.NextByte(tileDef.Variants))));
        var angle = random.NextAngle();
        var ends = new List<Vector2i>();

        for (var i = 0; i < dungen.Count; i++)
        {
            var remainingLength = dungen.Length;

            for (var x = remainingLength; x >= 0; x--)
            {
                position += angle.ToVec().Floored();
                angle += random.NextAngle(-dungen.MaxAngleChange, dungen.MaxAngleChange);
                var tile = new Tile(tileDef.TileId, variant: random.NextByte(tileDef.Variants));

                tiles.Add((position, tile));
                dungeon.CorridorTiles.Add(position);
            }

            ends.Add(position);
            position = tiles[random.Next(tiles.Count)].Index;
        }

        for (var i = 0; i < dungen.RoomCount; i++)
        {
            // TODO: Get room like how noise does
            position = random.PickAndTake(ends);
            // TODO: Rotation
            // TODO: Iterate room tiles and remove from corridors
            // TODO: patch holes
        }

        // TODO: Make corridor widening code re-usable.

        // Get boundary tiles of corridors as long as they don't intersect room 

        return dungeon;
    }
}
