using Robust.Client.Graphics;
using Robust.Shared.Player;
using Robust.Client.Player;
using Content.Client.Overlays;
using Content.Shared.Eye;

namespace Content.Client.Eye;

public sealed class DarkenedVisionSystem : SharedDarkenedVisionSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;


    private DarkenedVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkenedVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DarkenedVisionComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<DarkenedVisionComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<DarkenedVisionComponent, LocalPlayerDetachedEvent>(OnDetached);

        _overlay = new();
    }

    private void OnAttached(Entity<DarkenedVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
        _overlay.DarkenedVision = ent.Comp;
    }

    private void OnDetached(Entity<DarkenedVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.DarkenedVision = null;
    }

    private void OnStartup(Entity<DarkenedVisionComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity == ent.Owner)
        {
            _overlayMan.AddOverlay(_overlay);
            _overlay.DarkenedVision = ent.Comp;
        }
    }

    private void OnShutdown(Entity<DarkenedVisionComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
        {
            _overlayMan.RemoveOverlay(_overlay);
            _overlay.DarkenedVision = null;
        }
    }
}
