namespace Content.Server.Procedural.Rooms;

public sealed class SimpleRoomGen : IRoomGen
{
    public int Offset = 1;

    public List<DungeonRoom> GetRooms(List<Box2i> roomsList, Random random)
    {
        var rooms = new List<DungeonRoom>(roomsList.Count);

        foreach (var roomSpace in roomsList)
        {
            var room = new DungeonRoom();
            rooms.Add(room);

            for (var col = Offset; col < roomSpace.Width - Offset; col++)
            {
                for (var row = Offset; row < roomSpace.Height - Offset; row++)
                {
                    var position = roomSpace.BottomLeft + new Vector2i(col, row);
                    room.Tiles.Add(position);
                }
            }
        }

        return rooms;
    }
}
