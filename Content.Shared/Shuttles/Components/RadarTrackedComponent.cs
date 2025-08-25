using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent]
public sealed partial class RadarTrackedComponent : Component
{
    /// <summary>
    /// The shape the entity appears as on radar
    /// </summary>
    [DataField]
    public RadarSignatureShape Shape = RadarSignatureShape.Circle;

    /// <summary>
    /// Color of the indicator on the nav ui
    /// </summary>
    [DataField]
    public Color RadarColor { get; set; } = Color.FromHex("#FF0000");

    /// <summary>
    /// Rotation of the indicator on the nav ui
    /// </summary>
    [DataField]
    public Angle Angle = Angle.Zero;

    /// <summary>
    /// "radius" of the indicator on the nav ui
    /// </summary>
    [DataField]
    public float Size = 0.5f;
}

[Serializable, NetSerializable]
public enum RadarSignatureShape : byte
{
    Circle,
    Square,
    Triangle,
    Diamond,
    Chevron,
    Line
}
