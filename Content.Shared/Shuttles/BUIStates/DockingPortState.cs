using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

/// <summary>
/// State of each individual docking port for interface purposes
/// </summary>
[Serializable, NetSerializable]
public sealed class DockingPortState
{
    public string Name = string.Empty;

    public NetCoordinates Coordinates;
    public Angle Angle;
    public NetEntity Entity;
    public bool Connected => GridDockedWith != null;

    public NetEntity? GridDockedWith;

    /// <summary>
    /// The default colour used to shade a dock on a radar screen
    /// </summary>
    public Color Color;

    /// <summary>
    /// The colour used to shade a dock on a radar screen if it is highlighted (hovered over/selected on docking screen/shown in the main ship radar)
    /// </summary>
    public Color HighlightedColor;
}
