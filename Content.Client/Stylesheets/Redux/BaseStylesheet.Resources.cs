using System.Diagnostics.CodeAnalysis;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux;

public abstract partial class BaseStylesheet
{
    /// <summary>
    ///     The file roots of the stylesheet, dictates where assets get read from for the given type of resource.
    ///     Roots will be checked in order for assets, avoid having a significant number of them.
    /// </summary>
    /// <remarks>
    ///     Must be a constant, changes to this after construction will not be reflected.
    /// </remarks>
    public abstract Dictionary<Type, ResPath[]> Roots { get; }

    /// <summary>
    ///     Attempts to locate a resource within the stylesheet's roots.
    /// </summary>
    /// <param name="target">The relative path of the target resource.</param>
    /// <param name="resource">The discovered/cached resource, if any.</param>
    /// <typeparam name="T">Type of the resource to read.</typeparam>
    /// <returns>Whether <paramref name="resource"/> is null.</returns>
    public bool TryGetResource<T>(ResPath target, [NotNullWhen(true)] out T? resource)
        where T : BaseResource, new()
    {
        DebugTools.Assert(target.IsRelative,
            "Target path must be relative. Use ResCache directly if you need an absolute file location.");

        foreach (var root in Roots[typeof(T)])
        {
            if (ResCache.TryGetResource(root / target, out resource))
                return true;
        }

        resource = null;
        return false;
    }

    /// <summary>
    ///     Retrieves a resource, or throws.
    /// </summary>
    /// <param name="target">The relative path of the target resource.</param>
    /// <typeparam name="T">Type of the resource to read.</typeparam>
    /// <returns>The retrieved resource</returns>
    /// <exception cref="MissingResourceException">Thrown if the resource does not exist within the stylesheet's roots.</exception>
    public T GetResource<T>(ResPath target)
        where T : BaseResource, new()
    {
        if (TryGetResource(target, out T? res))
            return res;

        throw new MissingResourceException(this, target.ToString());
    }
}

/// <summary>
///     Exception thrown when the never-fail helpers in <see cref="PalettedStylesheet"/> fail to locate a resource.
/// </summary>
/// <param name="sheet">The stylesheet </param>
/// <param name="target"></param>
public sealed class MissingResourceException(BaseStylesheet sheet, string target) : Exception
{
    public override string Message =>
        $"Failed to find any resource at \"{target}\" for {sheet}. The roots are: {sheet.Roots}";

    public override string? Source => sheet.ToString();
}
