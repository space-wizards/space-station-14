using Content.Shared.Abilities;
using Content.Shared.DeltaV.CCVars;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.Nyanotrasen.Overlays;

public sealed partial class DogVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    private DogVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DogVisionComponent, ComponentInit>(OnDogVisionInit);
        SubscribeLocalEvent<DogVisionComponent, ComponentShutdown>(OnDogVisionShutdown);
        SubscribeLocalEvent<DogVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DogVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        Subs.CVar(_cfg, DCCVars.NoVisionFilters, OnNoVisionFiltersChanged);

        _overlay = new();
    }

    private void OnDogVisionInit(EntityUid uid, DogVisionComponent component, ComponentInit args)
    {
        if (uid == _playerMan.LocalEntity && !_cfg.GetCVar(DCCVars.NoVisionFilters))
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnDogVisionShutdown(EntityUid uid, DogVisionComponent component, ComponentShutdown args)
    {
        if (uid == _playerMan.LocalEntity)
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, DogVisionComponent component, LocalPlayerAttachedEvent args)
    {
        if (!_cfg.GetCVar(DCCVars.NoVisionFilters))
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, DogVisionComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnNoVisionFiltersChanged(bool enabled)
    {
        if (enabled)
            _overlayMan.RemoveOverlay(_overlay);
        else
            _overlayMan.AddOverlay(_overlay);
    }
}
