namespace Content.Shared.DV.Paper;

[RegisterComponent]
public sealed partial class ItemToggleSignatureWriterComponent : Component
{
    /// <summary>
    /// The font used when this item is activated.
    /// </summary>
    [DataField("activatedFont")]
    public string? ActivatedFont;

    /// <summary>
    /// The color used when this item is activated.
    /// </summary>
    [DataField("activatedColor")]
    public Color? ActivatedColor;

    /// <summary>
    /// The list of colors used when this item is activated.
    /// </summary>
    [DataField("activatedColorList")]
    public Dictionary<string, Color> ActivatedColorList = new();

    /// <summary>
    /// The font used when this item is deactivated.
    /// </summary>
    [DataField("deactivatedFont")]
    public string? DeactivatedFont;

    /// <summary>
    /// The color used when this item is deactivated.
    /// </summary>
    [DataField("deactivatedColor")]
    public Color? DeactivatedColor;

    /// <summary>
    /// The list of colors used when this item is deactivated.
    /// </summary>
    [DataField("deactivatedColorList")]
    public Dictionary<string, Color> DeactivatedColorList = new();
}
