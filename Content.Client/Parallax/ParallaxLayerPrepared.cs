using System;
using Robust.Client.Graphics;
using Content.Client.Parallax.Data;
using Robust.Shared.Graphics;

namespace Content.Client.Parallax;

/// <summary>
/// A 'prepared' (i.e. texture loaded and ready to use) parallax layer.
/// </summary>
public struct ParallaxLayerPrepared
{
    /// <summary>
    /// The loaded texture for this layer.
    /// </summary>
    public Texture Texture { get; set; }

    /// <summary>
    /// The configuration for this layer.
    /// </summary>
    public ParallaxLayerConfig Config { get; set; }
}

