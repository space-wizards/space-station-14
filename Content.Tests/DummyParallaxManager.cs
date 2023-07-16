using System.Numerics;
using Content.Client.Parallax;
using Content.Client.Parallax.Managers;

namespace Content.Tests
{
    public sealed class DummyParallaxManager : IParallaxManager
    {
        public Vector2 ParallaxAnchor { get; set; }
        public bool IsLoaded(string name)
        {
            return true;
        }

        public ParallaxLayerPrepared[] GetParallaxLayers(string name)
        {
            return Array.Empty<ParallaxLayerPrepared>();
        }

        public void LoadDefaultParallax()
        {
            return;
        }

        public Task LoadParallaxByName(string name)
        {
            return Task.CompletedTask;
        }

        public void UnloadParallax(string name)
        {
            return;
        }
    }
}
