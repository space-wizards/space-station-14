using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

public sealed partial class NightVisionOverlaySystem : EquipmentHudSystem<NightVisionComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    private NightVisionOverlay? _overlay;

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionComponent> component)
    {
        base.UpdateInternal(component);

        _lightManager.DrawLighting = false;
        if (component.Components.Count > 0)
        {
            _overlay = new NightVisionOverlay(component.Components[0]);
            _overlayMan.AddOverlay(_overlay);
        }
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
