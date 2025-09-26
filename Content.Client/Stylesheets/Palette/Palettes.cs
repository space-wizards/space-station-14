namespace Content.Client.Stylesheets.Palette;

/// <summary>
///     Stores all style palettes in one accessible location
/// </summary>
/// <remarks>
///     Technically not limited to only colors, can store like, standard padding amounts, and font sizes, maybe?
/// </remarks>
public static class Palettes
{
    // muted tones
    public static readonly ColorPalette Navy = new ColorPalette("#4f5376", lightnessShift: 0.05f, chromaShift: 0.0045f);
    public static readonly ColorPalette Cyan = new ColorPalette("#42586a", lightnessShift: 0.05f, chromaShift: 0.0045f);
    public static readonly ColorPalette Slate = new ColorPalette("#545562");
    public static readonly ColorPalette Neutral = new ColorPalette("#555555");

    // status tones
    public static readonly ColorPalette Red = new ColorPalette("#b62124", chromaShift: 0.02f);
    public static readonly ColorPalette Amber = new ColorPalette("#c18e36");
    public static readonly ColorPalette Green = new ColorPalette("#3c854a");
    public static readonly StatusPalette Status = new([Red.Base, Amber.Base, Green.Base]);

    // highlight tones
    public static readonly ColorPalette Gold = new ColorPalette("#a88b5e");
    public static readonly ColorPalette Maroon = new ColorPalette("#9b2236");

    // Intended to be used with `ModulateSelf` to darken / lighten something
    public static readonly ColorPalette AlphaModulate = new ColorPalette("#ffffff");

}
