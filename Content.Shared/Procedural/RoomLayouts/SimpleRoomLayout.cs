namespace Content.Shared.Procedural.RoomLayouts;

/// <summary>
/// Generates a square room up to the offset away from the room bounds.
/// </summary>
public sealed class SimpleRoomLayout : IRoomLayout
{
    /// <summary>
    /// Offset from the provided room bounds.
    /// </summary>
    [DataField("offset")] public int Offset = 1;
}
