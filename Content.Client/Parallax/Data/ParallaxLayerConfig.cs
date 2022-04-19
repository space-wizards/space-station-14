using System;
using Robust.Client.Graphics;
using Content.Client.Parallax.Data;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Parallax.Data;

/// <summary>
/// The configuration for a parallax layer.
/// </summary>
[DataDefinition]
public sealed class ParallaxLayerConfig
{
    /// <summary>
    /// The texture source for this layer.
    /// </summary>
    [DataField("texture", required: true)]
    public IParallaxTextureSource Texture { get; set; } = default!;

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
    [DataField("controlHomePos")]
    public Vector2 ControlHomePosition { get; set; }

    /// <summary>
    /// A world position such that if an Eye were positioned there, this parallax would be centred in the screen.
    /// Used for in-game.
    /// </summary>
    [DataField("worldHomePos")]
    public Vector2 WorldHomePosition { get; set; }

    /// <summary>
    /// Multiplier based on eye world position for this parallax layer.
    /// </summary>
    [DataField("slowness")]
    public float Slowness { get; set; } = 0.5f;
}

