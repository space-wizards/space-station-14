using Robust.Shared.Random;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robust = default!;

    public HashSet<Vector2i> RandomWalk(Vector2i start, int length)
    {
        return RandomWalk(start, length, _robust.Next());
    }

    public HashSet<Vector2i> RandomWalk(Vector2i start, int length, int seed)
    {
        // Don't pre-allocate length as it may be shorter than that due to backtracking.
        var path = new HashSet<Vector2i> { start };
        var previous = start;
        var random = new Random(seed);

        for (var i = 0; i < length; i++)
        {
            var randomDirection = (Direction) random.Next(8);
            var position = previous + randomDirection.ToIntVec();
            path.Add(position);
            previous = position;
        }

        return path;
    }

    public List<Box2i> BinarySpacePartition(Box2i bounds, Vector2i minSize, int seed)
    {
        var roomsQueue = new Queue<Box2i>();
        var rooms = new List<Box2i>();
        roomsQueue.Enqueue(bounds);
        var random = new Random(seed);
        var minWidth = minSize.X;
        var minHeight = minSize.Y;

        while (roomsQueue.Count > 0)
        {
            var room = roomsQueue.Dequeue();

            if (room.Height >= minSize.Y && room.Width >= minSize.X)
            {
                if (random.NextDouble() < 0.5)
                {
                    if (room.Height >= minSize.Y * 2)
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
                    if (room.Width >= minSize.X * 2)
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
        }

        return rooms;
    }

    private void SplitVertically(int minWidth, Queue<Box2i> roomsQueue, Box2i room, Random random)
    {
        var xSplit = random.Next(1, room.Width);
        var room1 = new Box2i(room.Left, room.Bottom, xSplit, room.Height);
        var room2 = new Box2i(room.Left + xSplit, room.Bottom, room.Width - xSplit, room.Height);
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }

    private void SplitHorizontally(int minHeight, Queue<Box2i> roomsQueue, Box2i room, Random random)
    {
        var ySplit = random.Next(1, room.Height);
        var room1 = new Box2i(room.Left, room.Bottom, room.Width, ySplit);
        var room2 = new Box2i(room.Left, room.Bottom + ySplit, room.Width, room.Height - ySplit);
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }

    /// <summary>
    /// Gets the wall boundaries for the specified tiles. Doesn't return any floor tiles.
    /// </summary>
    /// <param name="floors"></param>
    /// <returns></returns>
    public HashSet<Vector2i> GetWalls(HashSet<Vector2i> floors)
    {
        var walls = new HashSet<Vector2i>(floors.Count);

        foreach (var tile in floors)
        {
            for (var i = 0; i < 8; i++)
            {
                var direction = (Direction) i;
                var neighborTile = tile + direction.ToIntVec();
                if (floors.Contains(neighborTile))
                    continue;

                walls.Add(neighborTile);
            }
        }

        return walls;
    }
}
