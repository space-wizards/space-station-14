using System;
using SS14.Client.Graphics;

namespace Content.Client.Interfaces.Parallax
{
    public interface IParallaxManager
    {
        event Action<Texture> OnTextureLoaded;
        Texture ParallaxTexture { get; }
        void LoadParallax();
    }
}
