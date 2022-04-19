using System;
using Robust.Client.Graphics;
using Content.Client.Parallax;

namespace Content.Client.Parallax.Managers;

public interface IParallaxManager
{
    ParallaxLayerPrepared[] ParallaxLayers { get; }
    void LoadParallax();
}

