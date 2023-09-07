
using Content.Shared.Eye.Blinding;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Client.Eye.Blinding;

public sealed class MonochromacySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;


    private MonochromacyOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonochromacyComponent, ComponentInit>(OnMonochromacyInit);
        SubscribeLocalEvent<MonochromacyComponent, ComponentShutdown>(OnMonochromacyShutdown);

        SubscribeLocalEvent<MonochromacyComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MonochromacyComponent, PlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, MonochromacyComponent component, PlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, MonochromacyComponent component, PlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnMonochromacyInit(EntityUid uid, MonochromacyComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnMonochromacyShutdown(EntityUid uid, MonochromacyComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
