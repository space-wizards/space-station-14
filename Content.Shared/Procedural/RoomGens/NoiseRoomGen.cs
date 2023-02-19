namespace Content.Shared.Procedural.RoomGens;

/// <summary>
/// Uses random noise to generate a room.
/// </summary>
public sealed class NoiseRoomGen : RoomGen
{
    /// <summary>
    /// Should we use a square shape or a circle.
    /// </summary>
    public bool Box = false;

    // TODO: Add some noise params here
}
