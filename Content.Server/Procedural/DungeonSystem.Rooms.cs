using System.Linq;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Rooms;
using Robust.Shared.Noise;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    /// <summary>
    /// Adds simple walls around the boundary of a room.
    /// </summary>
    /// <param name="room"></param>
    private void AddBoundaryWalls(DungeonRoom room)
    {
        foreach (var tile in room.Tiles)
        {
            for (var j = 0; j < 8; j++)
            {
                var direction = (Direction) j;
                var neighbor = tile + direction.ToIntVec();

                if (room.Tiles.Contains(neighbor))
                    continue;

                room.Walls.Add(neighbor);
            }
        }
    }

    public Vector2i GetRoomCenter(DungeonRoom room)
    {
        var center = Vector2.Zero;

        if (room.Tiles.Count == 0)
            return (Vector2i) center;

        foreach (var tile in room.Tiles)
        {
            center += tile;
        }

        return (Vector2i) (center / room.Tiles.Count);
    }

    private List<DungeonRoom> GetNoiseRooms(NoiseRoomGen gen, float radius, Random random)
    {
        var noise = new FastNoiseLite(random.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        noise.SetFractalGain(0f);
        noise.SetFrequency(0.06f);
        var room = new DungeonRoom(string.Empty, string.Empty, new HashSet<Vector2i>(), new HashSet<Vector2i>());

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
                else
                {
                    room.Walls.Add(new Vector2i(x, y));
                }
            }
        }

        return new List<DungeonRoom>() { room };
    }

    private List<DungeonRoom> GetWormRooms(WormRoomGen gen, float radius, Random random)
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

        var room = new DungeonRoom(string.Empty, string.Empty, new HashSet<Vector2i>(), new HashSet<Vector2i>());

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

        AddBoundaryWalls(room);

        return new List<DungeonRoom>() { room };
    }

    private List<DungeonRoom> GetRandomWalkDungeon(RandomWalkRoomGen gen, float radius, Random random)
    {
        var start = gen.StartPosition;

        var currentPosition = start;
        var room = new DungeonRoom(string.Empty, string.Empty, new HashSet<Vector2i>(), new HashSet<Vector2i>());

        // TODO: Actually use radius
        for (var i = 0; i < gen.Iterations; i++)
        {
            var path = RandomWalk(currentPosition, gen.Length, random);
            room.Tiles.UnionWith(path);

            if (gen.StartRandomlyEachIteration)
                currentPosition = room.Tiles.ElementAt(random.Next(room.Tiles.Count));
        }

        AddBoundaryWalls(room);

        return new List<DungeonRoom>() { room };
    }

    private List<DungeonRoom> GetBSPRooms(BSPRoomGen gen, float radius, Random random)
    {
        var left = (int) Math.Floor(-radius);
        var bottom = (int) Math.Floor(-radius);

        var roomSpaces = BinarySpacePartition(new Box2i(left, bottom, left * -1, bottom * -1), gen.MinimumRoomDimensions, random, false);
        var rooms = GetRooms(gen.Rooms, roomSpaces, random);
        return rooms;
    }
}
