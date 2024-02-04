using Content.Client.Resources;
using Content.Shared.Shuttles.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    [Dependency] private readonly IResourceCache _resource = default!;

    public Texture GetTexture(Entity<ShuttleMapParallaxComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
        {
            return _resource.GetTexture(ShuttleMapParallaxComponent.FallbackTexture);
        }

        return _resource.GetTexture(entity.Comp.TexturePath);
    }
}
