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

        // Find the component with the lowest noise.
        NightVisionComponent? best = null;
        var bestNoise = float.MaxValue;
        foreach (var comp in component.Components)
        {
            var noise = comp.NoiseAmount * comp.NoiseMultiplier;
            if (noise < bestNoise)
            {
                bestNoise = noise;
                best = comp;
            }
        }

        _overlay = new NightVisionOverlay(best!.Color, best.NoiseAmount, best.NoiseMultiplier);
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
