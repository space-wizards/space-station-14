using Content.Shared.Suspicion;
using Robust.Client.Graphics;

namespace Content.Client.Suspicion;

public sealed class SuspicionRadarOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    public RadarInfo[] RadarInfos = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<OnSuspicionRadarOverlayToggledEvent>(OnOverlayToggled);
        SubscribeNetworkEvent<SuspicionRadarOverlayUpdatedEvent>(OnOverlayUpdate);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay<SuspicionRadarOverlay>();
    }

    private void OnOverlayToggled(OnSuspicionRadarOverlayToggledEvent ev)
    {
        if (ev.IsEnabled)
            _overlayManager.AddOverlay(new SuspicionRadarOverlay());
        else
            _overlayManager.RemoveOverlay<SuspicionRadarOverlay>();
    }

    private void OnOverlayUpdate(SuspicionRadarOverlayUpdatedEvent ev)
    {
        RadarInfos = ev.RadarInfos;
    }
}
