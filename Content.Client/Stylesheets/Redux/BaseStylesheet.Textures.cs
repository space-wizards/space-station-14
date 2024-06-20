using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux;

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

    /// <inheritdoc cref="M:Content.Client.Stylesheets.Redux.BaseStylesheet.TryGetTexture(Robust.Shared.Utility.ResPath,Robust.Client.Graphics.Texture@)"/>
    public bool TryGetTexture(string target, [NotNullWhen(true)] out Texture? texture)
    {
        return TryGetTexture(new ResPath(target), out texture);
    }

    /// <summary>
    ///     Retrieves a texture, or throws.
    /// </summary>
    /// <param name="target">The relative path of the target texture.</param>
    /// <returns>The retrieved texture</returns>
    /// <exception cref="MissingResourceException">Thrown if the texture does not exist within the stylesheet's roots.</exception>
    public Texture GetTexture(ResPath target)
    {
        return GetResource<TextureResource>(target).Texture;
    }

    public Texture GetTexture(string target)
    {
        return GetResource<TextureResource>(target).Texture;
    }

    #endregion
}
