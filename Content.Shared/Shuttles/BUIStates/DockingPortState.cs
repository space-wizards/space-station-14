using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

/// <summary>
/// State of each individual docking port for interface purposes
/// </summary>
[Serializable, NetSerializable]
public sealed class DockingPortState
{
    public NetCoordinates Coordinates;
    public Angle Angle;
    public NetEntity Entity;
    public bool Connected;
    public Color Color;
    public Color HighlightedColor;
}
