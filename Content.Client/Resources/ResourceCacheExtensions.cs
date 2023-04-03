using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Resources
{
    [PublicAPI]
    public static class ResourceCacheExtensions
    {
        public static Texture GetTexture(this IResourceCache cache, ResourcePath path)
        {
            return cache.GetResource<TextureResource>(path);
        }

        public static Texture GetTexture(this IResourceCache cache, string path)
        {
            return GetTexture(cache, new ResourcePath(path));
        }

        public static Font GetFont(this IResourceCache cache, ResourcePath path, int size)
        {
            return new VectorFont(cache.GetResource<FontResource>(path), size);
        }

        public static Font GetFont(this IResourceCache cache, string path, int size)
        {
            return cache.GetFont(new ResourcePath(path), size);
        }

        public static Font GetFont(this IResourceCache cache, ResourcePath[] path, int size)
        {
            var fs = new Font[path.Length];
            for (var i = 0; i < path.Length; i++)
                fs[i] = new VectorFont(cache.GetResource<FontResource>(path[i]), size);

            return new StackedFont(fs);
        }

        public static Font GetFont(this IResourceCache cache, string[] path, int size)
        {
            var rp = new ResourcePath[path.Length];
            for (var i = 0; i < path.Length; i++)
                rp[i] = new ResourcePath(path[i]);

            return cache.GetFont(rp, size);
        }
    }
}
