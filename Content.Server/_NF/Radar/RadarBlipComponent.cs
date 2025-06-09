using Content.Shared._NF.Radar;

namespace Content.Server._NF.Radar;

/// <summary>
/// Handles objects which should be represented by radar blips.
/// </summary>
[RegisterComponent]
public sealed partial class RadarBlipComponent : Component
{
    /// <summary>
    /// Color that gets shown on the radar screen.
    /// </summary>
    [DataField]
    public Color RadarColor { get; set; } = Color.Red;

    /// <summary>
    /// Color that gets shown on the radar screen when the blip is highlighted.
    /// </summary>
    [DataField]
    public Color HighlightedRadarColor { get; set; } = Color.OrangeRed;

    /// <summary>
    /// Scale of the blip.
    /// </summary>
    [DataField]
    public float Scale { get; set; } = 1f;

    /// <summary>
    /// The shape of the blip on the radar.
    /// </summary>
    [DataField]
    public RadarBlipShape Shape { get; set; } = RadarBlipShape.Circle;

    /// <summary>
    /// Whether this blip is enabled and should be shown on radar.
    /// </summary>
    [DataField]
    public bool Enabled { get; set; } = true;
}
