using Content.Shared.Eye.Blinding;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;

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

        SubscribeLocalEvent<BlurryVisionComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BlurryVisionComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<BlurryVisionComponent, ComponentHandleState>(OnHandleState);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, BlurryVisionComponent component, PlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, BlurryVisionComponent component, PlayerDetachedEvent args)
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

    private void OnHandleState(EntityUid uid, BlurryVisionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BlurryVisionComponentState state)
            return;

        component.Magnitude = state.Magnitude;
    }
}
