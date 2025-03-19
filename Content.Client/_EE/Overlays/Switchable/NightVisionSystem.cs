using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared._EE.Overlays.Switchable;
using Robust.Client.Graphics;

namespace Content.Client._EE.Overlays.Switchable;

public sealed class NightVisionSystem : Client.Overlays.EquipmentHudSystem<NightVisionComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    private BaseSwitchableOverlay<NightVisionComponent> _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, SwitchableOverlayToggledEvent>(OnToggle);

        _overlay = new BaseSwitchableOverlay<NightVisionComponent>();
    }

    protected override void OnRefreshComponentHud(Entity<NightVisionComponent> ent, ref RefreshEquipmentHudEvent<NightVisionComponent> args)
    {
        base.OnRefreshComponentHud(ent, ref args);
    }

    protected override void OnRefreshEquipmentHud(Entity<NightVisionComponent> ent, ref InventoryRelayedEvent<RefreshEquipmentHudEvent<NightVisionComponent>> args)
    {
        if (!ent.Comp.IsEquipment)
            return;

        base.OnRefreshEquipmentHud(ent, ref args);
    }

    private void OnToggle(Entity<NightVisionComponent> ent, ref SwitchableOverlayToggledEvent args)
    {
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionComponent> args)
    {
        base.UpdateInternal(args);

        var active = false;
        NightVisionComponent? nvComp = null;
        foreach (var comp in args.Components)
        {
            if (comp.IsActive || comp.PulseTime > 0f && comp.PulseAccumulator < comp.PulseTime)
                active = true;
            else
                continue;

            if (comp.DrawOverlay)
            {
                if (nvComp == null)
                    nvComp = comp;
                else if (nvComp.PulseTime > 0f && comp.PulseTime <= 0f)
                    nvComp = comp;
            }

            if (active && nvComp is { PulseTime: <= 0 })
                break;
        }

        UpdateNightVision(active);
        UpdateOverlay(nvComp);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        UpdateNightVision(false);
        UpdateOverlay(null);
    }

    private void UpdateNightVision(bool active)
    {
        _lightManager.DrawShadows = !active;
    }

    private void UpdateOverlay(NightVisionComponent? nvComp)
    {
        _overlay.Comp = nvComp;

        switch (nvComp)
        {
            case not null when !_overlayMan.HasOverlay<BaseSwitchableOverlay<NightVisionComponent>>():
                _overlayMan.AddOverlay(_overlay);
                break;
            case null:
                _overlayMan.RemoveOverlay(_overlay);
                break;
        }

        if (_overlayMan.TryGetOverlay<BaseSwitchableOverlay<ThermalVisionComponent>>(out var overlay))
            overlay.IsActive = nvComp == null;
    }
}
