using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets;

public abstract partial class BaseStylesheet
{
    #region Texture helpers

    /// <summary>
    ///     Attempts to locate a texture within the stylesheet's roots.
    /// </summary>
    /// <param name="target">The relative path of the target resource.</param>
    /// <param name="texture">The retrieved texture, if any.</param>
    /// <returns>Whether <paramref name="texture"/> is null.</returns>
    public bool TryGetTexture(ResPath target, [NotNullWhen(true)] out Texture? texture)
    {
        if (TryGetResource(target, out TextureResource? resource))
        {
            texture = resource.Texture;
            return true;
        }

        texture = null;
        return false;
    }

    /// <summary>
    ///     Retrieves a texture, or throws.
    /// </summary>
    /// <param name="target">The relative path of the target texture.</param>
    /// <returns>The retrieved texture</returns>
    /// <exception cref="MissingStyleResourceException">Thrown if the texture does not exist within the stylesheet's roots.</exception>
    public Texture GetTexture(ResPath target)
    {
        return GetResource<TextureResource>(target).Texture;
    }

    /// <summary>
    ///     Retrieves a texture, or falls back to the specified root. The resource should be present at the fallback
    ///     root, or else it throws
    /// </summary>
    /// <remarks>
    ///     This should be used to allow common sheetlets to be generic over multiple stylesheets without forcing other
    ///     styles to have the resource present, if your sheetlet is stylesheet-specific you should not use this.
    /// </remarks>
    /// <param name="target">The relative path of the target texture.</param>
    /// <param name="fallbackRoot">The root that this resource will always exist at</param>
    /// <returns>The retrieved texture</returns>
    /// <exception cref="ExpectedResourceException">Thrown if the texture does not exist in the fallback root.</exception>
    public Texture GetTextureOr(ResPath target, ResPath fallbackRoot)
    {
        return GetResourceOr<TextureResource>(target, fallbackRoot).Texture;
    }

    #endregion
}
