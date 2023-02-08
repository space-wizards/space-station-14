namespace Content.Shared.Procedural.RoomLayouts;

public sealed class RandomWalkRoomLayout : IRoomLayout
{
    [DataField("offset")] public int Offset = 2;

    [DataField("length")] public int Length = 10;

    [DataField("iterations")] public int Iterations = 10;

    [DataField("startRandom")] public bool StartRandom = true;
}
