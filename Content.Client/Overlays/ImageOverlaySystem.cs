using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;


/// <summary>
/// Adds image overlay when wearing item with ImageOverlayComponent
/// </summary>
public sealed class ImageOverlaySystem : EquipmentHudSystem<ImageOverlayComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private ImageOverlay _overlay = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImageOverlayComponent, ItemMaskToggledEvent>(OnItemToggled);
        _overlay = new();
    }

    private void OnItemToggled(Entity<ImageOverlayComponent> ent, ref ItemMaskToggledEvent args)
    {
        _overlay.OverlayActivate(ent.Comp, !args.Mask.Comp.IsToggled);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ImageOverlayComponent> component)
    {
        base.UpdateInternal(component);

        _overlay.UpdateState(component.Components);

        if (component.Components.Count > 0)
        {
            _overlayMan.AddOverlay(_overlay);
        }
        else
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.UpdateState(new List<ImageOverlayComponent>());
    }
}
