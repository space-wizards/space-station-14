namespace Content.Shared.Procedural.Rooms;

public sealed class WormRoomGen : IRoomGen
{
    [DataField("start")]
    public Vector2i StartPosition;

    [DataField("length")]
    public int Length = 50;

    [DataField("startDirection")]
    public Direction StartDirection = Direction.NorthEast;

    /// <summary>
    /// For the -1 -> 1 range of noise what does the max of each correspond to.
    /// </summary>
    [DataField("range")] public Angle Range = Angle.FromDegrees(90);

    /// <summary>
    /// Width of the worm.
    /// </summary>
    [DataField("width")]
    public int Width = 3;
}
