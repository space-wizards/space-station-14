namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    private HashSet<Vector2i> ConnectRooms(BSPDunGen gen, List<Vector2i> roomCenters, Random random)
    {
        var corridors = new HashSet<Vector2i>();
        var currentRoomCenter = roomCenters[random.Next(roomCenters.Count)];
        roomCenters.Remove(currentRoomCenter);

        while (roomCenters.Count > 0)
        {
            var closest = FindClosestPointTo(currentRoomCenter, roomCenters);
            roomCenters.Remove(closest);
            var newCorridor = CreateCorridor(gen.Corridors, currentRoomCenter, closest);
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

    public HashSet<Vector2i> RandomWalk(Vector2i start, int length, Random random)
    {
        // Don't pre-allocate length as it may be shorter than that due to backtracking.
        var path = new HashSet<Vector2i> { start };
        var previous = start;

        for (var i = 0; i < length; i++)
        {
            // Only want cardinals
            var randomDirection = (Direction) (random.Next(4) * 2);
            var position = previous + randomDirection.ToIntVec();
            path.Add(position);
            previous = position;
        }

        return path;
    }

    public List<Box2i> BinarySpacePartition(Box2i bounds, Vector2i minSize, Random random)
    {
        var roomsQueue = new Queue<Box2i>();
        var rooms = new List<Box2i>();
        roomsQueue.Enqueue(bounds);
        var minWidth = minSize.X;
        var minHeight = minSize.Y;

        while (roomsQueue.TryDequeue(out var room))
        {
            if (room.Height < minHeight || room.Width < minWidth)
                continue;

            if (random.NextDouble() < 0.5)
            {
                if (room.Height >= minHeight * 2)
                {
                    SplitHorizontally(minHeight, roomsQueue, room, random);
                }
                else if (room.Width >= minWidth * 2)
                {
                    SplitVertically(minWidth, roomsQueue, room, random);
                }
                else if (room.Width >= minWidth && room.Height >= minHeight)
                {
                    rooms.Add(room);
                }
            }
            else
            {
                if (room.Width >= minWidth * 2)
                {
                    SplitVertically(minWidth, roomsQueue, room, random);
                }
                else if (room.Height >= minHeight * 2)
                {
                    SplitHorizontally(minHeight, roomsQueue, room, random);
                }
                else if (room.Width >= minWidth && room.Height >= minHeight)
                {
                    rooms.Add(room);
                }
            }
        }

        return rooms;
    }

    private void SplitVertically(int minWidth, Queue<Box2i> roomsQueue, Box2i room, Random random)
    {
        // TODO: Config for 1, thing
        var xSplit = random.Next(minWidth, room.Width - minWidth);
        var room1 = new Box2i(room.Left, room.Bottom, room.Left + xSplit, room.Top);
        var room2 = new Box2i(room.Left + xSplit, room.Bottom, room.Right, room.Top);
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }

    private void SplitHorizontally(int minHeight, Queue<Box2i> roomsQueue, Box2i room, Random random)
    {
        var ySplit = random.Next(minHeight, room.Height - minHeight);
        var room1 = new Box2i(room.Left, room.Bottom, room.Right, room.Bottom + ySplit);
        var room2 = new Box2i(room.Left, room.Bottom + ySplit, room.Right, room.Top);
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }
}
