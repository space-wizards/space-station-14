using System;
using Content.Client.Parallax.Managers;
using Content.Client.Parallax;
using Robust.Client.Graphics;

namespace Content.IntegrationTests
{
    public sealed class DummyParallaxManager : IParallaxManager
    {
        public ParallaxLayerPrepared[] ParallaxLayers { get; } = {};

        public void LoadParallax()
        {
        }
    }
}
