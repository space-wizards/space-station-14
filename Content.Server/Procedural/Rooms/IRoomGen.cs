namespace Content.Server.Procedural.Rooms;

public interface IRoomGen
{
    List<DungeonRoom> GetRooms(List<Box2i> roomsList, Random random);
}
