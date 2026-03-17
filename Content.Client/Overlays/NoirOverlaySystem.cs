using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

public sealed partial class NoirOverlaySystem : EquipmentHudSystem<NoirOverlayComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private NoirOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NoirOverlayComponent> component)
    {
        base.UpdateInternal(component);

        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
