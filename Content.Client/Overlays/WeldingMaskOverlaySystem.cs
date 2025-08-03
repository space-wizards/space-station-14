using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;


/// <summary>
/// Adds a rectangular shader when wearing a welding mask or similar.
/// </summary>
public sealed partial class WeldingMaskOverlaySystem : EquipmentHudSystem<WeldingMaskOverlayComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public static readonly ProtoId<ShaderPrototype> ImageShader = "ImageMask";
    private ImageOverlay _overlay = new();

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<WeldingMaskOverlayComponent> component)
    {
        base.UpdateInternal(component);

        _overlay.ImageShaders.Clear();

        foreach (var comp in component.Components)
        {
            var values = new ImageShaderValues
            {
                PathToOverlayImage = comp.PathToOverlayImage,
                AdditionalOverlayAlpha = comp.AdditionalOverlayAlpha,
                AdditionalColor = comp.AdditionalColor
            };
            _overlay.ImageShaders.Add((_prototypeManager.Index(ImageShader).InstanceUnique(), values));
        }

        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlay.ImageShaders.Clear();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
