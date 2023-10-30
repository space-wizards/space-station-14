using Content.Shared.Eye.Blinding.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.Eye.Blinding;

public sealed class BlurryVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    private BlurryVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlurryVisionComponent, ComponentInit>(OnBlurryInit);
        SubscribeLocalEvent<BlurryVisionComponent, ComponentShutdown>(OnBlurryShutdown);

        SubscribeLocalEvent<BlurryVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BlurryVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, BlurryVisionComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, BlurryVisionComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnBlurryInit(EntityUid uid, BlurryVisionComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnBlurryShutdown(EntityUid uid, BlurryVisionComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
