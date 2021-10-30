using System;
using Robust.Client.Graphics;

namespace Content.Client.Parallax.Managers
{
    public interface IParallaxManager
    {
        event Action<Texture>? OnTextureLoaded;
        Texture? ParallaxTexture { get; }
        void LoadParallax();
    }
}
