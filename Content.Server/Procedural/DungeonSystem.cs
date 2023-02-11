using System.Linq;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Procedural.Rooms;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;

    public void SpawnDungeonTiles(Vector2i position, Dungeon dungeon, MapGridComponent grid, Random random, List<Vector2i> reservedTiles)
    {
        var tiles = new List<(Vector2i, Tile)>();

        foreach (var room in dungeon.Rooms)
        {
            var tileDef = _tileDef[room.Tile];
            var tileId = tileDef.TileId;

            foreach (var tile in room.Tiles)
            {
                var adjustedTilePos = tile + position;

                if (reservedTiles.Contains(adjustedTilePos))
                    continue;

                tiles.Add((adjustedTilePos, new Tile(tileId, variant: (byte) random.Next(tileDef.Variants))));
            }

            foreach (var tile in room.Walls)
            {
                var adjustedTilePos = tile + position;

                if (reservedTiles.Contains(adjustedTilePos))
                    continue;

                tiles.Add((adjustedTilePos, new Tile(tileId, variant: (byte) random.Next(tileDef.Variants))));
            }
        }

        foreach (var path in dungeon.Paths)
        {
            var tileDef = _tileDef[path.Tile];
            var tileId = tileDef.TileId;

            foreach (var tile in path.Tiles)
            {
                var adjustedTilePos = tile + position;

                if (reservedTiles.Contains(adjustedTilePos))
                    continue;

                tiles.Add((adjustedTilePos, new Tile(tileId, variant: (byte) random.Next(tileDef.Variants))));
            }
        }

        grid.SetTiles(tiles);
    }

    public Dungeon GetDungeon(DungeonConfigPrototype config, float radius, Random random)
    {
        var dungeon = new Dungeon();

        foreach (var roomConfig in config.Rooms)
        {
            List<DungeonRoom> rooms;

            switch (roomConfig)
            {
                case BSPRoomGen bsp:
                    rooms = GetBSPRooms(bsp, radius, random);
                    break;
                case NoiseRoomGen noisey:
                    rooms = GetNoiseRooms(noisey, radius, random);
                    break;
                case RandomWalkRoomGen walkies:
                    rooms = GetRandomWalkDungeon(walkies, radius, random);
                    break;
                case WormRoomGen worm:
                    rooms = GetWormRooms(worm, radius, random);
                    break;
                default:
                    throw new NotImplementedException();
            }

            foreach (var room in rooms)
            {
                room.Tile = roomConfig.Tile;
                room.Wall = roomConfig.Wall;
            }

            dungeon.Rooms.AddRange(rooms);
        }

        if (dungeon.Rooms.Count > 1)
        {
            foreach (var pathConfig in config.Paths)
            {
                var paths = GetPaths(dungeon, pathConfig, random);

                foreach (var path in paths)
                {
                    path.Tile = pathConfig.Tile;
                    path.Wall = pathConfig.Wall;
                }

                dungeon.Paths.AddRange(paths);
            }
        }

        return dungeon;
    }
}
