using System;
using Content.Client.Parallax.Managers;
using Content.Client.Parallax;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.IntegrationTests
{
    public sealed class DummyParallaxManager : IParallaxManager
    {
        public string ParallaxName { get; set; } = "";
        public Vector2 ParallaxAnchor { get; set; }
        public ParallaxLayerPrepared[] ParallaxLayers { get; } = {};

        public void LoadParallax()
        {
            ParallaxName = "default";
        }
    }
}
