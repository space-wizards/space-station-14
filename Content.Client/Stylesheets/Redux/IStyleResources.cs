using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux;

public interface IStyleResources
{
    /// <summary>
    ///     The file roots of the stylesheet, dictates where assets get read from for the given type of resource.
    ///     Roots will be checked in order for assets, avoid having a significant number of them.
    /// </summary>
    /// <remarks>
    ///     Must be a constant, changes to this after construction will not be reflected.
    /// </remarks>
    Dictionary<Type, ResPath[]> Roots { get; }

    /// <summary>
    ///     Attempts to locate a resource within the stylesheet's roots.
    /// </summary>
    /// <param name="target">The relative path of the target resource.</param>
    /// <param name="resource">The discovered/cached resource, if any.</param>
    /// <typeparam name="T">Type of the resource to read.</typeparam>
    /// <returns>Whether <paramref name="resource"/> is null.</returns>
    bool TryGetResource<T>(ResPath target, [NotNullWhen(true)] out T? resource)
        where T : BaseResource, new();

    /// <summary>
    ///     Retrieves a resource, or throws.
    /// </summary>
    /// <param name="target">The relative path of the target resource.</param>
    /// <typeparam name="T">Type of the resource to read.</typeparam>
    /// <returns>The retrieved resource</returns>
    /// <exception cref="MissingResourceException">Thrown if the resource does not exist within the stylesheet's roots.</exception>
    T GetResource<T>(ResPath target)
        where T : BaseResource, new();

    /// <summary>
    ///     Attempts to locate a texture within the stylesheet's roots.
    /// </summary>
    /// <param name="target">The relative path of the target resource.</param>
    /// <param name="texture">The retrieved texture, if any.</param>
    /// <returns>Whether <paramref name="texture"/> is null.</returns>
    bool TryGetTexture(ResPath target, [NotNullWhen(true)] out Texture? texture);

    /// <summary>
    ///     Retrieves a texture, or throws.
    /// </summary>
    /// <param name="target">The relative path of the target texture.</param>
    /// <returns>The retrieved texture</returns>
    /// <exception cref="MissingResourceException">Thrown if the texture does not exist within the stylesheet's roots.</exception>
    Texture GetTexture(ResPath target);
}
