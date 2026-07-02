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

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new NightVisionOverlay();

        SubscribeLocalEvent<NightVisionComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionComponent> component)
    {
        base.UpdateInternal(component);

        // Find the component with the lowest noise.
        NightVisionComponent? nvision = null;
        var bestNoise = float.MaxValue;
        foreach (var comp in component.Components)
        {
            if (!comp.Enabled)
                continue;

            var noise = comp.NoiseAmount * comp.NoiseMultiplier;
            if (noise < bestNoise)
            {
                nvision = comp;
                bestNoise = noise;
            }
        }

        // There is no active night vision components, so we disable the overlay.
        if (nvision == null)
        {
            DeactivateInternal();
            return;
        }

        _overlay.SetParameters(nvision.OverlayColor, nvision.LightingColor, nvision.NoiseAmount, nvision.NoiseMultiplier);

        if (!_overlayMan.HasOverlay<NightVisionOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnHandleState(Entity<NightVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    public void SetEnabled(Entity<NightVisionComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Enabled = enabled;

        RefreshOverlay();
    }
}
