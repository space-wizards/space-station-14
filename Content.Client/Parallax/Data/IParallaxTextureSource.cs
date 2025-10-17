using System.Threading;
using System.Threading.Tasks;
using Robust.Client.Graphics;

namespace Content.Client.Parallax.Data
{
    [ImplicitDataDefinitionForInheritors]
    public partial interface IParallaxTextureSource
    {
        /// <summary>
        /// Generates or loads the texture.
        /// Note that this should be cached, but not necessarily *here*.
        /// </summary>
        Task<Texture> GenerateTexture(CancellationToken cancel = default);

        /// <summary>
        /// Called when the parallax texture is no longer necessary, and may be unloaded.
        /// </summary>
        void Unload(IDependencyCollection dependencies)
        {
        }
    }
}

