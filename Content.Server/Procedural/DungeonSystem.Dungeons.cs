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
        var random = new Random();
        var roomSpaces = BinarySpacePartition(gen.Bounds, gen.MinimumRoomDimensions, random);
        List<DungeonRoom> rooms;

        if (true)
        {
            rooms = CreateRoomsRandomly(gen, roomSpaces, random);
        }
        else
        {
            rooms = CreateSimpleRooms(gen, roomSpaces, random);
        }

        var roomCenters = new List<Vector2i>();

        foreach (var room in roomSpaces)
        {
            roomCenters.Add((Vector2i) room.Center.Rounded());
        }

        var corridors = ConnectRooms(roomCenters, random);

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

    private List<DungeonRoom> CreateRoomsRandomly(BSPDunGen gen, List<Box2i> roomsList, Random random)
    {
        var rooms = new List<DungeonRoom>();

        for (var i = 0; i < roomsList.Count; i++)
        {
            var room = new DungeonRoom();
            rooms.Add(room);

            var roomBounds = roomsList[i];
            var center = roomBounds.Center;
            var roomCenter = new Vector2i((int) Math.Round(center.X), (int) Math.Round(center.Y));
            var roomFloor = RandomWalk(roomCenter, gen.Length, random);

            foreach (var position in roomFloor)
            {
                if (position.X >= roomBounds.Left + gen.Offset &&
                   position.X <= roomBounds.Right - gen.Offset &&
                   position.Y >= roomBounds.Bottom - gen.Offset &&
                   position.Y <= roomBounds.Top - gen.Offset)
                {
                    room.Tiles.Add(position);
                }
            }
        }

        return rooms;
    }

    private HashSet<Vector2i> ConnectRooms(List<Vector2i> roomCenters, Random random)
    {
        var corridors = new HashSet<Vector2i>();
        var currentRoomCenter = roomCenters[random.Next(roomCenters.Count)];
        roomCenters.Remove(currentRoomCenter);

        while (roomCenters.Count > 0)
        {
            var closest = FindClosestPointTo(currentRoomCenter, roomCenters);
            roomCenters.Remove(closest);
            var newCorridor = CreateCorridor(currentRoomCenter, closest);
            currentRoomCenter = closest;
            corridors.UnionWith(newCorridor);
        }
        return corridors;
    }

    private HashSet<Vector2i> CreateCorridor(Vector2i currentRoomCenter, Vector2i destination)
    {
        var corridor = new HashSet<Vector2i>();
        var position = currentRoomCenter;
        corridor.Add(position);

        while (position.Y != destination.Y)
        {
            if (destination.Y > position.Y)
            {
                position += Vector2i.Up;
            }
            else if (destination.Y < position.Y)
            {
                position += Vector2i.Down;
            }
            corridor.Add(position);
        }

        while (position.X != destination.X)
        {
            if (destination.X > position.X)
            {
                position += Vector2i.Right;
            }
            else if(destination.X < position.X)
            {
                position += Vector2i.Left;
            }
            corridor.Add(position);
        }
        return corridor;
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

    private List<DungeonRoom> CreateSimpleRooms(BSPDunGen gen, List<Box2i> roomsList, Random random)
    {
        var rooms = new List<DungeonRoom>(roomsList.Count);

        foreach (var roomSpace in roomsList)
        {
            var room = new DungeonRoom();
            rooms.Add(room);

            for (var col = gen.Offset; col < roomSpace.Width - gen.Offset; col++)
            {
                for (var row = gen.Offset; row < roomSpace.Height - gen.Offset; row++)
                {
                    var position = roomSpace.BottomLeft + new Vector2i(col, row);
                    room.Tiles.Add(position);
                }
            }
        }

        return rooms;
    }
}
