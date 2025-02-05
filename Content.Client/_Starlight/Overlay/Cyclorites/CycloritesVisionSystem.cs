using Content.Client.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Overlay.Cyclorites;

public sealed class CycloritesVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    private CycloritesVisionOverlay _overlay = default!;
    private EntityUid? _effect = null;
    private readonly EntProtoId _effectPrototype = "EffectNightVision";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CycloritesVisionComponent, ComponentInit>(OnBlurryInit);
        SubscribeLocalEvent<CycloritesVisionComponent, ComponentShutdown>(OnBlurryShutdown);
        SubscribeLocalEvent<CycloritesVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CycloritesVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, CycloritesVisionComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
        if (_effect == null)
        {
            _effect = SpawnAttachedTo(_effectPrototype, Transform(uid).Coordinates);
            _xformSys.SetParent(_effect.Value, uid);
        }
    }

    private void OnPlayerDetached(EntityUid uid, CycloritesVisionComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
         Del(_effect);
        _effect = null;
    }

    private void OnBlurryInit(EntityUid uid, CycloritesVisionComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.AddOverlay(_overlay);
            if (_effect == null)
            {
                _effect = SpawnAttachedTo(_effectPrototype, Transform(uid).Coordinates);
                _xformSys.SetParent(_effect.Value, uid);
            }
        }
    }

    private void OnBlurryShutdown(EntityUid uid, CycloritesVisionComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
            Del(_effect);
            _effect = null;
        }
    }
}
