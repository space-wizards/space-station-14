using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Client.Placement;

/// <summary>
/// Marker component that causes a directional arrow to be drawn during placement preview.
/// </summary>
[RegisterComponent]
public sealed partial class PlacementDirectionIndicatorComponent : Component
{
    /// <summary>
    /// Sprite to use as the direction indicator.
    /// </summary>
    [DataField]
    public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Markers/directional_arrow_indicator.rsi"), "blue_arrow");

    /// <summary>
    /// Color modulation applied to the indicator texture.
    /// </summary>
    [DataField]
    public Color Color = Color.White.WithAlpha(0.75f);

    /// <summary>
    /// Direction-relative offset applied to the indicator position.
    /// </summary>
    [DataField]
    public Vector2 Offset = new(0f, -0.8f);

    /// <summary>
    /// The angle at which the sprite texture naturally faces.
    /// Adjust this if your texture does not naturally point south.
    /// </summary>
    [DataField]
    public Angle SpriteNaturalAngle = Angle.Zero;
}
