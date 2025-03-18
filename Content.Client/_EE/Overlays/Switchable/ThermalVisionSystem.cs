using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared._EE.Overlays.Switchable;
using Robust.Client.Graphics;

namespace Content.Client._EE.Overlays.Switchable;

public sealed class ThermalVisionSystem : Client.Overlays.EquipmentHudSystem<ThermalVisionComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private ThermalVisionOverlay _thermalOverlay = default!;
    private BaseSwitchableOverlay<ThermalVisionComponent> _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, SwitchableOverlayToggledEvent>(OnToggle);

        _thermalOverlay = new ThermalVisionOverlay();
        _overlay = new BaseSwitchableOverlay<ThermalVisionComponent>();
    }

    protected override void OnRefreshComponentHud(Entity<ThermalVisionComponent> ent, ref RefreshEquipmentHudEvent<ThermalVisionComponent> args)
    {
        base.OnRefreshComponentHud(ent, ref args);
    }

    protected override void OnRefreshEquipmentHud(Entity<ThermalVisionComponent> ent, ref InventoryRelayedEvent<RefreshEquipmentHudEvent<ThermalVisionComponent>> args)
    {
        if (!ent.Comp.IsEquipment)
            return;

        base.OnRefreshEquipmentHud(ent, ref args);
    }

    private void OnToggle(Entity<ThermalVisionComponent> ent, ref SwitchableOverlayToggledEvent args)
    {
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ThermalVisionComponent> args)
    {
        base.UpdateInternal(args);
        ThermalVisionComponent? tvComp = null;
        var lightRadius = 0f;
        foreach (var comp in args.Components)
        {
            if (!comp.IsActive && (comp.PulseTime <= 0f || comp.PulseAccumulator >= comp.PulseTime))
                continue;

            if (tvComp == null)
                tvComp = comp;
            else if (!tvComp.DrawOverlay && comp.DrawOverlay)
                tvComp = comp;
            else if (tvComp.DrawOverlay == comp.DrawOverlay && tvComp.PulseTime > 0f && comp.PulseTime <= 0f)
                tvComp = comp;

            lightRadius = MathF.Max(lightRadius, comp.LightRadius);
        }

        _thermalOverlay.ResetLight(false);
        UpdateThermalOverlay(tvComp, lightRadius);
        UpdateOverlay(tvComp);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        UpdateOverlay(null);
        UpdateThermalOverlay(null, 0f);
    }

    private void UpdateThermalOverlay(ThermalVisionComponent? comp, float lightRadius)
    {
        _thermalOverlay.LightRadius = lightRadius;
        _thermalOverlay.Comp = comp;

        switch (comp)
        {
            case not null when !_overlayMan.HasOverlay<ThermalVisionOverlay>():
                _overlayMan.AddOverlay(_thermalOverlay);
                break;
            case null:
                _overlayMan.RemoveOverlay(_thermalOverlay);
                _thermalOverlay.ResetLight();
                break;
        }
    }

    private void UpdateOverlay(ThermalVisionComponent? tvComp)
    {
        _overlay.Comp = tvComp;

        switch (tvComp)
        {
            case { DrawOverlay: true } when !_overlayMan.HasOverlay<BaseSwitchableOverlay<ThermalVisionComponent>>():
                _overlayMan.AddOverlay(_overlay);
                break;
            case null or { DrawOverlay: false }:
                _overlayMan.RemoveOverlay(_overlay);
                break;
        }

        // Night vision overlay is prioritized
        _overlay.IsActive = !_overlayMan.HasOverlay<BaseSwitchableOverlay<NightVisionComponent>>();
    }
}
