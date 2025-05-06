using Content.Shared.Drowsiness;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Drowsiness;

public sealed class DrowsinessSystem : SharedDrowsinessSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private DrowsinessOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrowsinessStatusEffectComponent, ComponentInit>(OnDrowsinessInit);
        SubscribeLocalEvent<DrowsinessStatusEffectComponent, ComponentShutdown>(OnDrowsinessShutdown);

        SubscribeLocalEvent<DrowsinessStatusEffectComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DrowsinessStatusEffectComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, DrowsinessStatusEffectComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, DrowsinessStatusEffectComponent component, LocalPlayerDetachedEvent args)
    {
        _overlay.CurrentPower = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnDrowsinessInit(EntityUid uid, DrowsinessStatusEffectComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnDrowsinessShutdown(EntityUid uid, DrowsinessStatusEffectComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlay.CurrentPower = 0;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
