using System.Linq;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Rooms;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Random;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void SpawnDungeon(Vector2i position, Dungeon dungeon, DungeonConfigPrototype configPrototype,
        MapGridComponent grid)
    {
        SpawnDungeon(position, dungeon, configPrototype, grid, new List<Vector2i>());
    }

    public void SpawnDungeon(Vector2i position, Dungeon dungeon, DungeonConfigPrototype configPrototype, MapGridComponent grid, List<Vector2i> reservedTiles)
    {
        var tiles = new List<(Vector2i, Tile)>();
        var tileId = _tileDef[configPrototype.Tile].TileId;

        foreach (var room in dungeon.Rooms)
        {
            foreach (var tile in room.Tiles)
            {
                var adjustedTilePos = tile + position;
                tiles.Add((adjustedTilePos, new Tile(tileId)));
            }
        }

        foreach (var path in dungeon.Paths)
        {
            foreach (var tile in path.Tiles)
            {
                var adjustedTilePos = tile + position;
                tiles.Add((adjustedTilePos, new Tile(tileId)));
            }
        }

        foreach (var tile in dungeon.Walls)
        {
            var adjustedTilePos = tile + position;

            if (reservedTiles.Contains(adjustedTilePos))
                continue;

            tiles.Add((adjustedTilePos, new Tile(tileId)));
        }

        grid.SetTiles(tiles);

        foreach (var tile in dungeon.Walls)
        {
            var adjustedTilePos = tile + position;

            if (reservedTiles.Contains(adjustedTilePos))
                continue;

            Spawn(configPrototype.Wall, grid.GridTileToLocal(tile + position));
        }
    }

    public Dungeon GetDungeon(DungeonConfigPrototype config, float radius, Random random)
    {
        var dungeon = new Dungeon();

        foreach (var room in config.Rooms)
        {
            List<DungeonRoom> rooms;

            switch (room)
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

            dungeon.Rooms.AddRange(rooms);
        }

        return dungeon;
    }

    public List<DungeonRoom> GetNoiseRooms(NoiseRoomGen gen, float radius, Random random)
    {
        var noise = new FastNoiseLite(random.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        noise.SetFractalGain(0f);
        noise.SetFrequency(0.04f);
        var room = new DungeonRoom(new HashSet<Vector2i>());

        for (var x = (int) Math.Floor(-radius); x < Math.Ceiling(radius); x++)
        {
            for (var y = (int) Math.Floor(-radius); y < Math.Ceiling(radius); y++)
            {
                if (!gen.Box)
                {
                    var indices = new Vector2(x + 0.5f, y + 0.5f);

                    if (indices.Length > radius)
                        continue;
                }

                var value = noise.GetNoise(x, y);
                if (value >= 0.1f)
                {
                    room.Tiles.Add(new Vector2i(x, y));
                }
            }
        }

        return new List<DungeonRoom>() { room };
    }

    public List<DungeonRoom> GetWormRooms(WormRoomGen gen, float radius, Random random)
    {
        // TODO: Tunnel to thingo has 2 tile gap + also needs to force tiles
        var noise = new FastNoiseLite(random.Next());
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.02f);
        var current = gen.StartPosition;
        var direction = gen.StartDirection;
        var angle = direction.ToAngle();
        var baseTiles = new HashSet<Vector2i> { current };

        // TODO: Steering strength based on distance

        for (var i = 0; i < gen.Length; i++)
        {
            var node = current + direction.ToIntVec();

            if (((Vector2) (node - current)).Length >= radius)
            {
                direction = direction.GetOpposite();
                continue;
            }

            var rotation = noise.GetNoise(node.X, node.Y);
            var randAngle = gen.Range * rotation;
            angle += randAngle;
            direction = angle.GetDir();
            baseTiles.Add(node);
            current = node;
        }

        var room = new DungeonRoom(new HashSet<Vector2i>());

        foreach (var tile in baseTiles)
        {
            room.Tiles.Add(tile);

            for (var x = 0; x < gen.Width; x++)
            {
                for (var y = 0; y < gen.Width; y++)
                {
                    var neighbor = new Vector2i(tile.X + x, tile.Y + y);
                    room.Tiles.Add(neighbor);
                }
            }
        }

        return new List<DungeonRoom>() { room };
    }

    public List<DungeonRoom> GetRandomWalkDungeon(RandomWalkRoomGen gen, float radius, Random random)
    {
        var start = gen.StartPosition;

        var currentPosition = start;
        var room = new DungeonRoom(new HashSet<Vector2i>());

        for (var i = 0; i < gen.Iterations; i++)
        {
            var path = RandomWalk(currentPosition, gen.Length, random);
            room.Tiles.UnionWith(path);

            if (gen.StartRandomlyEachIteration)
                currentPosition = room.Tiles.ElementAt(random.Next(room.Tiles.Count));
        }

        return new List<DungeonRoom>() { room };
    }

    public List<DungeonRoom> GetBSPRooms(BSPRoomGen gen, float radius, Random random)
    {
        var left = (int) Math.Floor(-radius);
        var bottom = (int) Math.Floor(-radius);

        var roomSpaces = BinarySpacePartition(new Box2i(left, bottom, left * -1, bottom * -1), gen.MinimumRoomDimensions, random, false);
        var rooms = GetRooms(gen.Rooms, roomSpaces, random);
        return rooms;
    }
}
