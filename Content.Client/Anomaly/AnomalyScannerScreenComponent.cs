using Robust.Client.Graphics;

namespace Content.Client.Anomaly;

/// <summary>
/// This component creates and handles the drawing of a ScreenTexture to be used on the Anomaly Scanner
/// for an indicator of Anomaly Severity.
/// TODO: I would like to refactor this as something like "DynamicTextureComponent" that can contain a dictionary
/// of texture components like "Bar(offset, size, minimumValue, maximumValue, AppearanceKey, LayerMapKey)" that can just
/// draw a bar that will show up on a texture layer.
/// </summary>
[RegisterComponent]
public sealed partial class AnomalyScannerScreenComponent : Component
{
    public OwnedTexture? ScreenTexture = null;

    [DataField]
    public Vector2i Offset =  new Vector2i(12, 17);

    [DataField]
    public Vector2i Size = new Vector2i(10, 3);
}
