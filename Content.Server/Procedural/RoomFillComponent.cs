using Content.Shared.Procedural;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

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
    /// Size of the room to fill.
    /// </summary>
    [DataField(required: true)]
    public Vector2i Size;

    /// <summary>
    /// Rooms allowed for the marker.
    /// </summary>
    [DataField]
    public EntityWhitelist? RoomWhitelist;
    
    /// <summary>
    /// Should any existing entities / decals be bulldozed first.
    /// </summary>
    [DataField]
    public bool ClearExisting;
}
