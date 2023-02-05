using System.Linq;
using Content.Shared.Procedural;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

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

                if (reservedTiles.Contains(adjustedTilePos))
                    continue;

                tiles.Add((adjustedTilePos, new Tile(tileId)));
            }
        }

        foreach (var tile in dungeon.Corridors)
        {
            var adjustedTilePos = tile + position;

            if (reservedTiles.Contains(adjustedTilePos))
                continue;

            tiles.Add((tile + position, new Tile(tileId)));
        }

        foreach (var tile in dungeon.Walls)
        {
            var adjustedTilePos = tile + position;

            if (reservedTiles.Contains(adjustedTilePos))
                continue;

            tiles.Add((tile + position, new Tile(tileId)));
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

    public Dungeon GetDungeon(IDungeonGenerator gen)
    {
        switch (gen)
        {
            case BSPDunGen bsp:
                return GetBSPDungeon(bsp);
            case RandomWalkDunGen walkies:
                return GetRandomWalkDungeon(walkies);
            default:
                throw new NotImplementedException();
        }
    }

    public Dungeon GetRandomWalkDungeon(RandomWalkDunGen gen)
    {
        var random = new Random();
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

    public Dungeon GetBSPDungeon(BSPDunGen gen)
    {
        if (gen.Bounds.IsEmpty())
        {
            throw new InvalidOperationException();
        }

        var random = new Random();
        var roomSpaces = BinarySpacePartition(gen.Bounds, gen.MinimumRoomDimensions, random);
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
