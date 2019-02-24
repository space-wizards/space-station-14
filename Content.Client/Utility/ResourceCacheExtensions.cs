using JetBrains.Annotations;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.Utility;

namespace Content.Client.Utility
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
    }
}
