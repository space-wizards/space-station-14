using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.Overlays;

public sealed partial class BlackAndWhiteOverlaySystem : EquipmentHudSystem<BlackAndWhiteOverlayComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private BlackAndWhiteOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<BlackAndWhiteOverlayComponent> component)
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
