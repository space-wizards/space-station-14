using System.Diagnostics.CodeAnalysis;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets;

public abstract partial class StylesheetFactory
{
    /// <summary>
    /// The file roots of the stylesheet, dictates where assets get read from for the given type of resource.
    /// Roots will be checked in order for assets, avoid having a significant number of them.
    /// </summary>
    /// <remarks>
    /// Must be a constant, changes to this after construction will not be reflected.
    /// </remarks>
    public abstract Dictionary<Type, ResPath[]> Roots { get; }

    /// <summary>
    /// Attempts to locate a resource within the stylesheet's roots.
    /// </summary>
    /// <param name="target">The relative path of the target resource.</param>
    /// <param name="resource">The discovered/cached resource, if any.</param>
    /// <typeparam name="T">Type of the resource to read.</typeparam>
    /// <returns>Whether <paramref name="resource"/> is null.</returns>
    public bool TryGetResource<T>(ResPath target, [NotNullWhen(true)] out T? resource)
        where T : BaseResource, new()
    {
        DebugTools.Assert(target.IsRelative, "Target path must be relative.");

        foreach (var root in Roots[typeof(T)])
        {
            if (_resourceCache.TryGetResource(root / target, out resource))
                return true;
        }

        resource = null;
        return false;
    }

    /// <summary>
    /// Retrieves a resource, or throws.
    /// </summary>
    /// <param name="target">The relative path of the target resource.</param>
    /// <typeparam name="T">Type of the resource to read.</typeparam>
    /// <returns>The retrieved resource</returns>
    /// <exception cref="MissingStyleResourceException">Thrown if the resource does not exist within the stylesheet's roots.</exception>
    public T GetResource<T>(ResPath target)
        where T : BaseResource, new()
    {
        return TryGetResource(target, out T? res)
            ? res
            : throw new MissingStyleResourceException(this, target.ToString(), Roots.ToString());
    }

    /// <summary>
    ///     Retrieves a resource, or falls back to the specified root. The resource should be present at the fallback
    ///     root, or else it throws
    /// </summary>
    /// <remarks>
    ///     This should be used to allow common sheetlets to be generic over multiple stylesheets without forcing other
    ///     styles to have the resource present, if your sheetlet is stylesheet-specific you should not use this.
    /// </remarks>
    /// <param name="target">The relative path of the target resource.</param>
    /// <param name="fallbackRoot">The root that this resource will always exist at</param>
    /// <typeparam name="T">Type of the resource to read.</typeparam>
    /// <returns>The retrieved resource</returns>
    /// <exception cref="ExpectedResourceException">Thrown if the resource does not exist in the fallback root.</exception>
    [Obsolete("Use GetResource/TryGetResource instead and add fallback path to StylesheetFactory.Roots.")]
    public T GetResourceOr<T>(ResPath target, ResPath fallbackRoot)
        where T : BaseResource, new()
    {
        DebugTools.Assert(fallbackRoot.IsRooted,
            "Fallback root must be absolute. Roots can be retrieved from the stylesheets.");

        if (!_resourceCache.TryGetResource(fallbackRoot / target, out T? fallback))
            throw new ExpectedResourceException(this, target.ToString());

        return TryGetResource(target, out T? res) ? res : fallback;
    }

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
    [Obsolete("Use GetTexture/TryGetTexture instead and add fallback path to StylesheetFactory.Roots.")]
    public Texture GetTextureOr(ResPath target, ResPath fallbackRoot)
    {
        return GetResourceOr<TextureResource>(target, fallbackRoot).Texture;
    }
}

/// <summary>
/// Exception thrown when the never-fail helpers in <see cref="StylesheetFactory"/> fail to locate a resource.
/// </summary>
/// <param name="sheet">The stylesheet </param>
/// <param name="target"></param>
public sealed class MissingStyleResourceException(StylesheetFactory sheet, string target, string? roots) : Exception
{
    public override string Message =>
        $"Failed to find any resource at \"{target}\" for {sheet}. The roots are: {roots}";

    public override string? Source => sheet.ToString();
}

/// <summary>
/// Exception thrown when the never-fail helpers in <see cref="StylesheetFactory"/> expect a resource at a location
/// but do not find it.
/// </summary>
/// <param name="sheet">The stylesheet</param>
/// <param name="target"></param>
public sealed class ExpectedResourceException(StylesheetFactory sheet, string target) : Exception
{
    public override string Message =>
        $"Failed to find any resource at \"{target}\" for {sheet}, when such a resource was expected.";

    public override string? Source => sheet.ToString();
}
