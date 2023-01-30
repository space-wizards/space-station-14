namespace Content.Server.Procedural;

[DataDefinition]
public sealed class BSPDunGen : IDungeonGenerator
{
    [DataField("bounds")] public Box2i Bounds;

    [DataField("min")]
    public Vector2i MinimumRoomDimensions;

    // TODO: Move this to the simple one
    /// <summary>
    /// Boundary to the BSP border.
    /// </summary>
    [DataField("offset")]
    public int Offset = 1;

    /// <summary>
    /// Random walk length
    /// </summary>
    [DataField("length")] public int Length;
}
