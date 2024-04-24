using Content.Shared.Abilities;
using Content.Shared.DeltaV.CCVars;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.Nyanotrasen.Overlays;

public sealed partial class DogVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private DogVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DogVisionComponent, ComponentInit>(OnDogVisionInit);
        SubscribeLocalEvent<DogVisionComponent, ComponentShutdown>(OnDogVisionShutdown);

        Subs.CVar(_cfg, DCCVars.NoVisionFilters, OnNoVisionFiltersChanged);

        _overlay = new();
    }

    private void OnDogVisionInit(EntityUid uid, DogVisionComponent component, ComponentInit args)
    {
        if (!_cfg.GetCVar(DCCVars.NoVisionFilters))
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnDogVisionShutdown(EntityUid uid, DogVisionComponent component, ComponentShutdown args)
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
