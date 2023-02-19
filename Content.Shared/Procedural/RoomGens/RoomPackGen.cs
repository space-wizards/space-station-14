namespace Content.Shared.Procedural.RoomGens;

/// <summary>
/// Generates a dungeon layout based on pre-defined areas (room packs) and then
/// chooses
/// </summary>
public sealed class RoomPackGen : RoomGen
{
    // TODO: Need a test to validate these dimensions.

    /// <summary>
    /// Area for room packs we can use.
    /// </summary>
    [DataField("roomPacks", required: true)]
    public List<Box2i> RoomPacks = new();
}
