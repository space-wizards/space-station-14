using Robust.Client.Graphics;

namespace Content.Client._Starlight.Overlay;

public sealed class ThermalVisionEntityHighlightOverlay : BaseEntityHighlightOverlay
{
    public ThermalVisionEntityHighlightOverlay(ShaderPrototype shader) : base(shader) { ZIndex = (int?)OverlayZIndexes.ThermalVisionEntityHighlight; }
}
