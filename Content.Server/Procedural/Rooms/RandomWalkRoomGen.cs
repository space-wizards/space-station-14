using System.Linq;

namespace Content.Server.Procedural.Rooms;

public sealed class RandomWalkRoomGen : IRoomGen
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public int Offset = 1;
    public int Length = 10;
    public bool StartRandom = true;

    public List<DungeonRoom> GetRooms(List<Box2i> roomsList, Random random)
    {
        var rooms = new List<DungeonRoom>();
        var genSystem = _entManager.System<DungeonSystem>();

        for (var i = 0; i < roomsList.Count; i++)
        {
            var room = new DungeonRoom();
            rooms.Add(room);
            var floors = new HashSet<Vector2i>();

            var roomBounds = roomsList[i];
            var center = roomBounds.Center;
            var roomCenter = new Vector2i((int) Math.Round(center.X), (int) Math.Round(center.Y));
            var currentPosition = roomCenter;

            for (var j = 0; j < 10; j++)
            {
                var path = genSystem.RandomWalk(currentPosition, Length, random);
                floors.UnionWith(path);

                if (StartRandom)
                    currentPosition = floors.ElementAt(random.Next(floors.Count));
            }

            foreach (var position in floors)
            {
                if (position.X >= roomBounds.Left + Offset &&
                   position.X <= roomBounds.Right - Offset &&
                   position.Y >= roomBounds.Bottom - Offset &&
                   position.Y <= roomBounds.Top - Offset)
                {
                    room.Tiles.Add(position);
                }
            }
        }

        return rooms;
    }
}
