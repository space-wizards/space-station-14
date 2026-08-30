using Robust.Client.Graphics;

namespace Content.Client.Graphics;

/// <summary>
/// A cache for <see cref="Overlay"/>s to store per-viewport render resources, such as render targets.
/// </summary>
/// <typeparam name="T">The type of data stored in the cache.</typeparam>
public sealed class OverlayResourceCache<T> : IDisposable where T : class, IDisposable
{
    private readonly Dictionary<long, CacheEntry> _cache = new();

    /// <summary>
    /// Get the data for a specific viewport, creating a new entry if necessary.
    /// </summary>
    /// <remarks>
    /// The cached data may be cleared at any time if <see cref="IClydeViewport.ClearCachedResources"/> gets invoked.
    /// </remarks>
    /// <param name="viewport">The viewport for which to retrieve cached data.</param>
    /// <param name="factory">A delegate used to create the cached data, if necessary.</param>
    public T GetForViewport(IClydeViewport viewport, Func<IClydeViewport, T> factory)
    {
        return GetForViewport(viewport, out _, factory);
    }

    /// <summary>
    /// Get the data for a specific viewport, creating a new entry if necessary.
    /// </summary>
    /// <remarks>
    /// The cached data may be cleared at any time if <see cref="IClydeViewport.ClearCachedResources"/> gets invoked.
    /// </remarks>
    /// <param name="viewport">The viewport for which to retrieve cached data.</param>
    /// <param name="wasCached">True if the data was pulled from cache, false if it was created anew.</param>
    /// <param name="factory">A delegate used to create the cached data, if necessary.</param>
    public T GetForViewport(IClydeViewport viewport, out bool wasCached, Func<IClydeViewport, T> factory)
    {
        if (_cache.TryGetValue(viewport.Id, out var entry))
        {
            wasCached = true;
            return entry.Data;
        }

        wasCached = false;

        entry = new CacheEntry
        {
            Data = factory(viewport),
            Viewport = new WeakReference<IClydeViewport>(viewport),
        };
        _cache.Add(viewport.Id, entry);

        viewport.ClearCachedResources += ViewportOnClearCachedResources;

        return entry.Data;
    }

    private void ViewportOnClearCachedResources(ClearCachedViewportResourcesEvent ev)
    {
        if (!_cache.Remove(ev.ViewportId, out var entry))
        {
            // I think this could theoretically happen if you manually dispose the cache *after* a leaked viewport got
            // GC'd, but before its ClearCachedResources got invoked.
            return;
        }

        entry.Data.Dispose();

        if (ev.Viewport != null)
            ev.Viewport.ClearCachedResources -= ViewportOnClearCachedResources;
    }

    public void Dispose()
    {
        foreach (var entry in _cache)
        {
            if (entry.Value.Viewport.TryGetTarget(out var viewport))
                viewport.ClearCachedResources -= ViewportOnClearCachedResources;

            entry.Value.Data.Dispose();
        }

        _cache.Clear();
    }

    private struct CacheEntry
    {
        public T Data;
        public WeakReference<IClydeViewport> Viewport;
    }
}
