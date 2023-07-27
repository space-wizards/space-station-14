using Content.Shared.Blob;
using Robust.Client.Graphics;

namespace Content.Client.Blob;

public sealed class BlobObserverSystem : SharedBlobObserverSystem
{
    [Dependency] private readonly ILightManager _lightManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobObserverComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlobObserverComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, BlobObserverComponent observerComponent, ComponentStartup args)
    {
        _lightManager.DrawLighting = false;
    }

    private void OnShutdown(EntityUid uid, BlobObserverComponent observerComponent, ComponentShutdown args)
    {
        _lightManager.DrawLighting = true;
    }
}
