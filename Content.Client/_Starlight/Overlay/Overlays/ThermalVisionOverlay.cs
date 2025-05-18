using Robust.Client.Graphics;

namespace Content.Client._Starlight.Overlay;

public sealed class ThermalVisionOverlay : BaseVisionOverlay
{
    public ThermalVisionOverlay(ShaderPrototype shader) : base(shader) { ZIndex = (int?)OverlayZIndexes.ThermalVision; }
}
