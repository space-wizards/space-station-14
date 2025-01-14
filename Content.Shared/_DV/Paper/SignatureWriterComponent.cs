namespace Content.Shared.DV.Paper;

[RegisterComponent]
public sealed partial class SignatureWriterComponent : Component
{
    /// <summary>
    /// If this is set, the defined font will be forced for the signature.
    /// </summary>
    [DataField("font")]
    public string? Font;

    /// <summary>
    /// The list of fonts that can be selected from, for pens with multiple fonts.
    /// </summary>
    [DataField("fontList")]
    public Dictionary<string, string> FontList = new();

    /// <summary>
    /// The color used for the signature.
    /// </summary>
    [DataField("color")]
    public Color Color = Color.FromHex("#2F4F4F");

    /// <summary>
    /// The list of colors that can be selected from, for pens with multiple colors.
    /// </summary>
    [DataField("colorList")]
    public Dictionary<string, Color> ColorList = new();
}
