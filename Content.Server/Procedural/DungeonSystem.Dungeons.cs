namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    public Dungeon GetRandomWalkDungeon(RandomWalkDunGen gen)
    {
        // TODO: Random
        var start = gen.StartPosition;
        var length = gen.Length;

        var floors = RandomWalk(start, length);
        var walls = GetWalls(floors);
        return new Dungeon()
        {
            Tiles = floors,
            Walls = walls,
        };
    }

    public Dungeon GetBSPDungeon(BSPDunGen gen)
    {
        var random = new Random();
        var rooms = BinarySpacePartition(gen.Bounds, gen.MinimumRoomDimensions, _robust.Next());
        var floor = new HashSet<Vector2i>();

        if (true)
        {
            floor = CreateRoomsRandomly(gen, rooms);
        }
        else
        {
            floor = CreateSimpleRooms(gen, rooms);
        }

        var roomCenters = new List<Vector2i>();

        foreach (var room in rooms)
        {
            roomCenters.Add((Vector2i) room.Center.Rounded());
        }

        var corridors = ConnectRooms(roomCenters);
        floor.UnionWith(corridors);

        return new Dungeon
        {
            Tiles = floor,
            Walls = GetWalls(floor)
        };
    }

    private HashSet<Vector2i> CreateRoomsRandomly(BSPDunGen gen, List<Box2i> roomsList)
    {
        var floor = new HashSet<Vector2i>();

        for (var i = 0; i < roomsList.Count; i++)
        {
            var roomBounds = roomsList[i];
            var center = roomBounds.Center;
            var roomCenter = new Vector2i((int) Math.Round(center.X), (int) Math.Round(center.Y));
            var roomFloor = RandomWalk(randomWalkParameters, roomCenter);

            foreach (var position in roomFloor)
            {
                if (position.X >= roomBounds.Left + gen.Offset &&
                   position.X <= roomBounds.Right - gen.Offset &&
                   position.Y >= roomBounds.Bottom - gen.Offset &&
                   position.Y <= roomBounds.Top - gen.Offset)
                {
                    floor.Add(position);
                }
            }
        }

        return floor;
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

    private HashSet<Vector2i> CreateSimpleRooms(BSPDunGen gen, List<Box2i> roomsList)
    {
        var floor = new HashSet<Vector2i>(roomsList.Count * 4);

        foreach (var room in roomsList)
        {
            for (var col = gen.Offset; col < room.Width - gen.Offset; col++)
            {
                for (var row = gen.Offset; row < room.Height - gen.Offset; row++)
                {
                    var position = room.BottomLeft + new Vector2i(col, row);
                    floor.Add(position);
                }
            }
        }

        return floor;
    }
}
