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

        SubscribeLocalEvent<DrowsinessComponent, ComponentInit>(OnDrowsinessInit);
        SubscribeLocalEvent<DrowsinessComponent, ComponentShutdown>(OnDrowsinessShutdown);

        SubscribeLocalEvent<DrowsinessComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DrowsinessComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, DrowsinessComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, DrowsinessComponent component, LocalPlayerDetachedEvent args)
    {
        _overlay.CurrentPower = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnDrowsinessInit(EntityUid uid, DrowsinessComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnDrowsinessShutdown(EntityUid uid, DrowsinessComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlay.CurrentPower = 0;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
