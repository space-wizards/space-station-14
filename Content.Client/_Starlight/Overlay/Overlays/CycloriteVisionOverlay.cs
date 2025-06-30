using Robust.Client.Graphics;

namespace Content.Client._Starlight.Overlay;

public sealed class CycloriteVisionOverlay : BaseVisionOverlay
{
    public CycloriteVisionOverlay(ShaderPrototype shader) : base(shader) { ZIndex = (int?)OverlayZIndexes.Cyclorite; }
}
