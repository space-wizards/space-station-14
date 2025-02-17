using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared.DeadSpace.NightVision;
using Content.Shared.GameTicking;
using Robust.Shared.Player;

namespace Content.Client.DeadSpace.NightVision;

public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] ILightManager _lightManager = default!;

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnNightVisionInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnNightVisionShutdown);

        SubscribeLocalEvent<NightVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NightVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, NightVisionComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, NightVisionComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _lightManager.DrawLighting = true;
    }

    private void OnNightVisionInit(EntityUid uid, NightVisionComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnNightVisionShutdown(EntityUid uid, NightVisionComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _lightManager.DrawLighting = true;
    }
}
