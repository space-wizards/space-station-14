namespace Content.Server.Procedural.Rooms;

public sealed class SimpleRoomGen : IRoomGen
{
    [DataField("offset")]
    public int Offset = 1;
}
