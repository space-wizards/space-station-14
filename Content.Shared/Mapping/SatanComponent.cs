using Robust.Shared.Serialization;

namespace Content.Shared.Mapping;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SatanAlignComponent : Component
{
    [DataField]
    public SatanTag AlignType = SatanTag.WallOrDoor;

    // Never change these
    public float ProximityMin = 0.45f; //TODO:ERRANT should these be system constants?
    public float ProximityMax = 1.1f;
}

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SatanKindComponent : Component
{
    [DataField]
    public SatanTag AlignType = SatanTag.WallOrDoor;
}

public enum SatanTag : byte
{
    /// <summary>
    /// Tile-sized walls, doors, mineable rock etc.
    /// Everything that would be functionally considered a likely neighbor for a door/airlock
    /// Does not include firelocks
    /// Does NOT include thin walls or doors
    /// </summary>
    WallOrDoor,
}
