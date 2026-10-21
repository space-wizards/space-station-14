using Robust.Client.Graphics;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Anomaly;

/// <summary>
/// This component creates and handles the drawing of a ScreenTexture to be used on the Anomaly Scanner
/// for an indicator of Anomaly Severity.
/// </summary>
/// <remarks>
/// In the future I would like to make this a more generic "DynamicTextureComponent" that can contain a dictionary
/// of texture components like "Bar(offset, size, minimumValue, maximumValue, AppearanceKey, LayerMapKey)" that can
/// just draw a bar or other basic drawn element that will show up on a texture layer.
/// </remarks>
[RegisterComponent]
[Access(typeof(AnomalyScannerSystem))]
public sealed partial class AnomalyScannerScreenComponent : Component
{
    /// <summary>
    /// This is the texture drawn as a layer on the Anomaly Scanner device.
    /// </summary>
    public OwnedTexture? ScreenTexture;

    /// <summary>
    /// A small buffer that we can reuse to draw the severity bar.
    /// </summary>
    public Rgba32[]? BarBuf;

    /// <summary>
    /// The position of the top-left of the severity bar in pixels.
    /// </summary>
    [DataField(readOnly: true)]
    public Vector2i Offset = new Vector2i(12, 17);

    /// <summary>
    /// The width and height of the severity bar in pixels.
    /// </summary>
    [DataField(readOnly: true)]
    public Vector2i Size = new Vector2i(10, 3);
}
