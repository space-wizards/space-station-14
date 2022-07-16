using System;
using Content.Client.Parallax.Managers;
using Content.Client.Parallax;
using Robust.Shared.Maths;

namespace Content.IntegrationTests
{
    public sealed class DummyParallaxManager : IParallaxManager
    {
        public Vector2 ParallaxAnchor { get; set; }
        public ParallaxLayerPrepared[] GetParallaxLayers(string name)
        {
            return Array.Empty<ParallaxLayerPrepared>();
        }

        public void LoadDefaultParallax()
        {
            return;
        }
    }
}
