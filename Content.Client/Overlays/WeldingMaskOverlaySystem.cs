using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class WeldingMaskOverlaySystem : EquipmentHudSystem<WeldingMaskOverlayComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private RectangleOverlay _overlay = new();

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<WeldingMaskOverlayComponent> component)
    {
        base.UpdateInternal(component);

        _overlay.RectangleShaders.Clear();

        foreach (var comp in component.Components)
        {
            var values = new RectangleShaderValues();

            values.OuterRectangleHeight = comp.OuterRectangleHeight;
            values.OuterRectangleWidth = comp.OuterRectangleWidth;
            values.InnerRectangleThickness = comp.InnerRectangleThickness;
            values.OuterAlpha = comp.OuterAlpha;
            values.InnerAlpha = comp.InnerAlpha;

            _overlay.RectangleShaders.Add((_prototypeManager.Index<ShaderPrototype>("GradientRectangleMask").InstanceUnique(), values));
        }

        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlay.RectangleShaders.Clear();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
