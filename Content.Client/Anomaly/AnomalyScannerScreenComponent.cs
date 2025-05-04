using Robust.Client.Graphics;

namespace Content.Client.Anomaly;

[RegisterComponent]
public sealed partial class AnomalyScannerScreenComponent : Component
{
    public OwnedTexture? ScreenTexture = null;

    [DataField]
    public Vector2i Offset =  new Vector2i(12, 16);
}
