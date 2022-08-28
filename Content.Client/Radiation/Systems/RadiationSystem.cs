using Content.Client.Radiation.Overlays;
using Content.Shared.Radiation.Systems;
using Robust.Client.Graphics;

namespace Content.Client.Radiation.Systems;

public sealed class RadiationSystem : SharedRadiationSystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RadiationUpdate>(OnUpdate);
        SubscribeNetworkEvent<RadiationRaysUpdate>(OnRayUpdate);
        SubscribeNetworkEvent<RadiationGridcastUpdate>(OnGridcastUpdate);
    }

    private void OnUpdate(RadiationUpdate ev)
    {
        if (!_overlayMan.TryGetOverlay(out RadiationViewOverlay? overlay) || overlay == null)
            return;
        overlay._radiationMap = ev.RadiationMap;
        overlay.SpaceMap = ev.SpaceMap;

    }

    private void OnRayUpdate(RadiationRaysUpdate ev)
    {
        if (!_overlayMan.TryGetOverlay(out RadiationRayOverlay? overlay) || overlay == null)
            return;
        overlay.Rays = ev.Rays;
    }

    private void OnGridcastUpdate(RadiationGridcastUpdate ev)
    {
        if (!_overlayMan.TryGetOverlay(out RadiationGridcastOverlay? overlay) || overlay == null)
            return;
        overlay.Rays = ev.Rays;
    }

}
