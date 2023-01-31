namespace Content.Server.Procedural.Rooms;

public sealed class RandomWalkRoomGen : IRoomGen
{
    [DataField("offset")] public int Offset = 1;

    [DataField("length")] public int Length = 10;

    [DataField("iterations")] public int Iterations = 10;

    [DataField("startRandom")] public bool StartRandom = true;
}
