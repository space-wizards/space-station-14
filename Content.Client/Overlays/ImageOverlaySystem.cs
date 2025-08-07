using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;


/// <summary>
/// Adds image overlay when wearing item with ImageOverlayComponent
/// </summary>
public sealed partial class ImageOverlaySystem : EquipmentHudSystem<ImageOverlayComponent>
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

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ImageOverlayComponent> component)
    {
        base.UpdateInternal(component);

        _overlay.TupleOfImageShaders.Clear();

        foreach (var comp in component.Components)
        {
            var values = new ImageShaderValues
            {
                PathToOverlayImage = comp.PathToOverlayImage,
                AdditionalColorOverlay = comp.AdditionalColorOverlay
            };
            _overlay.TupleOfImageShaders.Add((_prototypeManager.Index(ImageShader).InstanceUnique(), values));
        }

        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlay.TupleOfImageShaders.Clear();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
