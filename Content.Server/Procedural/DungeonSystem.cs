using System.Linq;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Dungeons;
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
        InitializeCommand();
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

        foreach (var tile in dungeon.Corridors)
        {
            var adjustedTilePos = tile + position;
            tiles.Add((adjustedTilePos, new Tile(tileId)));
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

    public Dungeon GetDungeon(IDungeonGenerator gen, Random random)
    {
        switch (gen)
        {
            case BSPDunGen bsp:
                return GetBSPDungeon(bsp, random);
            case NoiseDunGen noisey:
                return GetNoiseDungeon(noisey, random);
            case RandomWalkDunGen walkies:
                return GetRandomWalkDungeon(walkies, random);
            case WormDunGen worm:
                return GetWormDungeon(worm, random);
            default:
                throw new NotImplementedException();
        }
    }

    public Dungeon GetNoiseDungeon(NoiseDunGen gen, Random random)
    {
        var noise = new FastNoise(random.Next());
        noise.SetFractalType(FastNoise.FractalType.Ridged);
        noise.SetFractalGain(0f);
        noise.SetFrequency(0.04f);
        var room = new DungeonRoom();
        var walls = new HashSet<Vector2i>();

        for (var x = gen.Bounds.Left; x < gen.Bounds.Right; x++)
        {
            for (var y = gen.Bounds.Bottom; y < gen.Bounds.Top; y++)
            {
                var value = noise.GetNoise(x, y);
                if (value >= 0.1f)
                {
                    room.Tiles.Add(new Vector2i(x, y));
                }
                else
                {
                    walls.Add(new Vector2i(x, y));
                }
            }
        }

        return new Dungeon()
        {
            Rooms = new List<DungeonRoom>() { room },
            AllTiles = new HashSet<Vector2i>(room.Tiles),
            Walls = walls,
            Corridors = new HashSet<Vector2i>(),
        };
    }

    public Dungeon GetWormDungeon(WormDunGen gen, Random random)
    {
        // TODO: Tunnel to thingo has 2 tile gap + also needs to force tiles
        var noise = new FastNoise(random.Next());
        noise.SetNoiseType(FastNoise.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.02f);
        var current = gen.StartPosition;
        var direction = gen.StartDirection;
        var angle = direction.ToAngle();
        var room = new DungeonRoom();
        var baseTiles = new HashSet<Vector2i>();
        baseTiles.Add(current);

        for (var i = 0; i < gen.Length; i++)
        {
            var node = current + direction.ToIntVec();
            var rotation = noise.GetNoise(node.X, node.Y);
            var randAngle = gen.Range * rotation;
            angle += randAngle;
            direction = angle.GetDir();
            baseTiles.Add(node);
            current = node;
        }

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

        return new Dungeon
        {
            Rooms = new List<DungeonRoom>() { room },
            Corridors = new HashSet<Vector2i>(),
            AllTiles = new HashSet<Vector2i>(room.Tiles),
            Walls = new HashSet<Vector2i>(),
        };
    }

    public Dungeon GetRandomWalkDungeon(RandomWalkDunGen gen, Random random)
    {
        var start = gen.StartPosition;

        var currentPosition = start;
        var floors = new HashSet<Vector2i>();

        for (var i = 0; i < gen.Iterations; i++)
        {
            var path = RandomWalk(currentPosition, gen.Length, random);
            floors.UnionWith(path);

            if (gen.StartRandomlyEachIteration)
                currentPosition = floors.ElementAt(random.Next(floors.Count));
        }

        var walls = GetWalls(gen.Walls, new Box2i(), floors);
        floors.UnionWith(walls);

        return new Dungeon()
        {
            AllTiles = floors,
            Rooms = new List<DungeonRoom>(1)
            {
                new()
                {
                    Tiles = floors
                }
            },
            Walls = walls,
        };
    }

    public Dungeon GetBSPDungeon(BSPDunGen gen, Random random)
    {
        if (gen.Bounds.IsEmpty())
        {
            throw new InvalidOperationException();
        }

        var roomSpaces = BinarySpacePartition(gen.Bounds, gen.MinimumRoomDimensions, random, false);
        var rooms = GetRooms(gen.Rooms, roomSpaces, random);
        var roomCenters = new List<Vector2i>();

        foreach (var room in roomSpaces)
        {
            roomCenters.Add((Vector2i) room.Center.Rounded());
        }

        var corridors = ConnectRooms(gen, roomCenters, random);

        var allTiles = new HashSet<Vector2i>(corridors);

        foreach (var room in rooms)
        {
            allTiles.UnionWith(room.Tiles);
        }

        var walls = GetWalls(gen.Walls, gen.Bounds, allTiles);
        allTiles.UnionWith(walls);

        return new Dungeon
        {
            AllTiles = allTiles,
            Corridors = corridors,
            Rooms = rooms,
            Walls = walls,
        };
    }
}
