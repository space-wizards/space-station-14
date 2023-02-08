using System.Linq;
using Content.Shared.Procedural;
using Content.Shared.Procedural.RoomLayouts;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    // Why aren't these on the classes themselves? Coz dependencies.

    public List<DungeonRoom> GetRooms(IRoomLayout layout, List<Box2i> roomsList, Random random)
    {
        switch (layout)
        {
            case RandomWalkRoomLayout walk:
                return GetRooms(walk, roomsList, random);
            case SimpleRoomLayout simple:
                return GetRooms(simple, roomsList, random);
            default:
                throw new NotImplementedException();
        }
    }

    public List<DungeonRoom> GetRooms(RandomWalkRoomLayout layout, List<Box2i> roomsList, Random random)
    {
        var rooms = new List<DungeonRoom>();
        for (var i = 0; i < roomsList.Count; i++)
        {
            var room = new DungeonRoom(new HashSet<Vector2i>());
            rooms.Add(room);
            var floors = new HashSet<Vector2i>();

            var roomBounds = roomsList[i];
            var center = roomBounds.Center;
            var roomCenter = new Vector2i((int) Math.Round(center.X), (int) Math.Round(center.Y));
            var currentPosition = roomCenter;

            for (var j = 0; j < layout.Iterations; j++)
            {
                var path = RandomWalk(currentPosition, layout.Length, random);
                floors.UnionWith(path);

                if (layout.StartRandom)
                    currentPosition = floors.ElementAt(random.Next(floors.Count));
            }

            foreach (var position in floors)
            {
                if (position.X >= roomBounds.Left + layout.Offset &&
                    position.X <= roomBounds.Right - layout.Offset &&
                    position.Y >= roomBounds.Bottom - layout.Offset &&
                    position.Y <= roomBounds.Top - layout.Offset)
                {
                    room.Tiles.Add(position);
                }
            }
        }

        return rooms;
    }

    public List<DungeonRoom> GetRooms(SimpleRoomLayout layout, List<Box2i> roomsList, Random random)
    {
        var rooms = new List<DungeonRoom>(roomsList.Count);

        foreach (var roomSpace in roomsList)
        {
            var room = new DungeonRoom(new HashSet<Vector2i>());
            rooms.Add(room);

            for (var col = layout.Offset; col < roomSpace.Width - layout.Offset; col++)
            {
                for (var row = layout.Offset; row < roomSpace.Height - layout.Offset; row++)
                {
                    var position = roomSpace.BottomLeft + new Vector2i(col, row);
                    room.Tiles.Add(position);
                }
            }
        }

        return rooms;
    }
}
