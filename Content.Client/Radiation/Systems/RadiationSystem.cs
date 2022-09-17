using Content.Client.Radiation.Overlays;
using Content.Shared.Radiation.Events;
using Robust.Client.Graphics;

namespace Content.Client.Radiation.Systems;

public sealed class RadiationSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<OnRadiationOverlayToggledEvent>(OnOverlayToggled);
        SubscribeNetworkEvent<OnRadiationOverlayUpdateEvent>(OnOverlayUpdate);
        SubscribeNetworkEvent<OnRadiationOverlayResistanceUpdateEvent>(OnResistanceUpdate);
    }

    private void OnOverlayToggled(OnRadiationOverlayToggledEvent ev)
    {
        if (ev.IsEnabled)
            _overlayMan.AddOverlay(new RadiationDebugOverlay());
        else
            _overlayMan.RemoveOverlay<RadiationDebugOverlay>();
    }

    private void OnOverlayUpdate(OnRadiationOverlayUpdateEvent ev)
    {
        if (!_overlayMan.TryGetOverlay(out RadiationDebugOverlay? overlay))
            return;

        var str = $"Radiation update: {ev.ElapsedTimeMs}ms with. Receivers: {ev.ReceiversCount}, " +
                  $"Sources: {ev.SourcesCount}, Rays: {ev.Rays.Count}";
        Logger.Info(str);
        overlay.Rays = ev.Rays;
    }

    private void OnResistanceUpdate(OnRadiationOverlayResistanceUpdateEvent ev)
    {
        if (!_overlayMan.TryGetOverlay(out RadiationDebugOverlay? overlay))
            return;
        overlay.ResistanceGrids = ev.Grids;
    }
}
