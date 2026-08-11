using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

/// <summary>
/// Adds image overlay when wearing item with <see cref="ImageOverlayComponent"/>
/// </summary>
public sealed partial class ImageOverlaySystem : EquipmentHudSystem<ImageOverlayComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private ImageOverlay _overlay = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        _overlay = new();
    }

    [SubscribeLocalEvent]
    private void OnItemToggled(Entity<ImageOverlayComponent> ent, ref ItemMaskToggledEvent args)
    {
        _overlay.OverlayActivate(ent.Comp, !args.Mask.Comp.IsToggled);
    }

    /// <inheritdoc />
    protected override void UpdateInternal(RefreshEquipmentHudEvent<ImageOverlayComponent> component)
    {
        base.UpdateInternal(component);
        if (component.Components.Count == 0)
        {
            _overlayMan.RemoveOverlay(_overlay);
            return;
        }

        _overlayMan.AddOverlay(_overlay);
        _overlay.UpdateState(component.Components);
    }

    /// <inheritdoc />
    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
