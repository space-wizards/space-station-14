using System.Linq;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
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

        var walls = GetWalls(floors);

        return new Dungeon()
        {
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
        var rooms = gen.Rooms.GetRooms(roomSpaces, random);
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

        return new Dungeon
        {
            Corridors = corridors,
            Rooms = rooms,
            Walls = GetWalls(allTiles)
        };
    }

    private HashSet<Vector2i> ConnectRooms(BSPDunGen gen, List<Vector2i> roomCenters, Random random)
    {
        var corridors = new HashSet<Vector2i>();
        var currentRoomCenter = roomCenters[random.Next(roomCenters.Count)];
        roomCenters.Remove(currentRoomCenter);

        while (roomCenters.Count > 0)
        {
            var closest = FindClosestPointTo(currentRoomCenter, roomCenters);
            roomCenters.Remove(closest);
            var newCorridor = gen.Corridors.CreateCorridor(currentRoomCenter, closest);
            currentRoomCenter = closest;
            corridors.UnionWith(newCorridor);
        }
        return corridors;
    }

    private Vector2i FindClosestPointTo(Vector2i currentRoomCenter, List<Vector2i> roomCenters)
    {
        var closest = Vector2i.Zero;
        var distance = float.MaxValue;
        var roomCenter = (Vector2) currentRoomCenter;

        foreach (var position in roomCenters)
        {
            var currentDistance = (position - roomCenter).Length;

            if(currentDistance < distance)
            {
                distance = currentDistance;
                closest = position;
            }
        }

        return closest;
    }
}
