using System.Numerics;

namespace Content.Client.Parallax.Data;

/// <summary>
/// The configuration for a parallax layer.
/// </summary>
[DataDefinition]
public sealed partial class ParallaxLayerConfig
{
    /// <summary>
    /// The texture source for this layer.
    /// </summary>
    [DataField("texture", required: true)]
    public IParallaxTextureSource Texture { get; set; } = default!;

    /// <summary>
    /// A scaling factor for the texture.
    /// In the interest of simplifying maths, this is rounded down to integer for ParallaxControl, so be careful.
    /// </summary>
    [DataField("scale")]
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// If true, this layer is tiled as the camera scrolls around.
    /// If false, this layer only shows up around it's home position.
    /// </summary>
    [DataField("tiled")]
    public bool Tiled { get; set; } = true;

    /// <summary>
    /// A position relative to the centre of a ParallaxControl that this parallax should be drawn at, in pixels.
    /// Used for menus.
    /// Note that this is ignored if the parallax layer is tiled - in that event a random pixel offset is used and slowness is applied.
    /// </summary>
    [DataField("controlHomePosition")]
    public Vector2 ControlHomePosition { get; set; }

    /// <summary>
    /// The "relative to ParallaxAnchor" starting world position for this layer.
    /// Essentially, an unclamped lerp occurs between here and the eye position, with Slowness as the factor.
    /// Used for in-game.
    /// </summary>
    [DataField("worldHomePosition")]
    public Vector2 WorldHomePosition { get; set; }

    /// <summary>
    /// An adjustment performed to the world position of this layer after parallax shifting.
    /// Used for in-game.
    /// Useful for moving around Slowness = 1.0 objects (which can't otherwise be moved from screen centre).
    /// </summary>
    [DataField("worldAdjustPosition")]
    public Vector2 WorldAdjustPosition { get; set; }

    /// <summary>
    /// Multiplier of parallax shift.
    /// A slowness of 0.0f anchors this layer to the world.
    /// A slowness of 1.0f anchors this layer to the camera.
    /// </summary>
    [DataField("slowness")]
    public float Slowness { get; set; } = 0.5f;

    /// <summary>
    /// Should the parallax scroll at a specific rate per second.
    /// </summary>
    [DataField("scrolling")] public Vector2 Scrolling = Vector2.Zero;

    [DataField("shader")] public string? Shader = "unshaded";
}

