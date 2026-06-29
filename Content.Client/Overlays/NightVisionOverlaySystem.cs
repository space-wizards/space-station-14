using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

/// <summary>
/// Shows/hides the <see cref="NightVisionOverlay"/> based on whether the observed
/// entity has a <see cref="NightVisionComponent"/> equipped.
/// </summary>
public sealed partial class NightVisionOverlaySystem : EquipmentHudSystem<NightVisionComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private ILightManager _lightManager = default!;

    private NightVisionOverlay? _overlay;

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionComponent> component)
    {
        base.UpdateInternal(component);

        _lightManager.DrawLighting = false;
        if (component.Components.Count <= 0)
            return;

        var comp = component.Components[0];
        _overlay = new NightVisionOverlay(comp.Color, comp.NoiseAmount, comp.NoiseMultiplier);
        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _lightManager.DrawLighting = true;
        if (_overlay != null)
            _overlayMan.RemoveOverlay(_overlay);

        _overlay = null;
    }
}
