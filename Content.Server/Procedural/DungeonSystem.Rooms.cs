using System.Linq;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Rooms;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    // Why aren't these on the classes themselves? Coz dependencies.

    public List<DungeonRoom> GetRooms(IRoomGen gen, List<Box2i> roomsList, Random random)
    {
        switch (gen)
        {
            case RandomWalkRoomGen walk:
                return GetRooms(walk, roomsList, random);
                break;
            case SimpleRoomGen simple:
                return GetRooms(simple, roomsList, random);
            default:
                throw new NotImplementedException();
        }
    }

    public List<DungeonRoom> GetRooms(RandomWalkRoomGen gen, List<Box2i> roomsList, Random random)
    {
        var rooms = new List<DungeonRoom>();
        for (var i = 0; i < roomsList.Count; i++)
        {
            var room = new DungeonRoom();
            rooms.Add(room);
            var floors = new HashSet<Vector2i>();

            var roomBounds = roomsList[i];
            var center = roomBounds.Center;
            room.Center = center;
            var roomCenter = new Vector2i((int) Math.Round(center.X), (int) Math.Round(center.Y));
            var currentPosition = roomCenter;

            for (var j = 0; j < gen.Iterations; j++)
            {
                var path = RandomWalk(currentPosition, gen.Length, random);
                floors.UnionWith(path);

                if (gen.StartRandom)
                    currentPosition = floors.ElementAt(random.Next(floors.Count));
            }

            foreach (var position in floors)
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

    public List<DungeonRoom> GetRooms(SimpleRoomGen gen, List<Box2i> roomsList, Random random)
    {
        var rooms = new List<DungeonRoom>(roomsList.Count);

        foreach (var roomSpace in roomsList)
        {
            var room = new DungeonRoom();
            rooms.Add(room);
            room.Center = roomSpace.Center;

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
