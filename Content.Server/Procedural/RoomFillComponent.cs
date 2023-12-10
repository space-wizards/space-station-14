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
    /// Are rotated variants of rooms allowed?
    /// </summary>
    [DataField]
    public bool Rotations = true;

    /// <summary>
    /// Rooms allowed for the marker.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist RoomWhitelist;
}
