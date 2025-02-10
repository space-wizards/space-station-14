using Content.Shared.Whitelist;

namespace Content.Server.Procedural;

/// <summary>
/// Marker that indicates the specified room prototype should occupy this space.
/// </summary>
[RegisterComponent]
public sealed partial class RoomFillComponent : Component
{
    /// <summary>
    /// Are we allowed to rotate room templates?
    /// If the room is not a square this will only do 180 degree rotations.
    /// </summary>
    [DataField]
    public bool Rotation = true;

    /// <summary>
    /// Min size of the possible selected room.
    /// </summary>
    [DataField]
    public Vector2i MinSize = new (3, 3);

    /// <summary>
    /// Max size of the possible selected room.
    /// </summary>
    [DataField]
    public Vector2i MaxSize = new (10, 10);

    /// <summary>
    /// Rooms allowed for the marker.
    /// </summary>
    [DataField]
    public EntityWhitelist? RoomWhitelist;

    /// <summary>
    /// Should any existing entities / decals be bulldozed first.
    /// </summary>
    [DataField]
    public bool ClearExisting = true;
}
