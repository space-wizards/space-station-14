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
    public static readonly ColorPalette Navy = new ColorPalette("#4f5376"){ LightnessShift = 0.05f, ChromaShift = 0.0045f }.Construct();
    public static readonly ColorPalette Cyan = new ColorPalette("#42586a"){ LightnessShift = 0.05f, ChromaShift = 0.0045f }.Construct();
    public static readonly ColorPalette Slate = new ColorPalette("#545562").Construct();
    public static readonly ColorPalette Neutral = new ColorPalette("#555555").Construct();

    // status tones
    public static readonly ColorPalette Red = new ColorPalette("#b62124"){ ChromaShift = 0.02f }.Construct();
    public static readonly ColorPalette Amber = new ColorPalette("#c18e36").Construct();
    public static readonly ColorPalette Green = new ColorPalette("#3c854a").Construct();
    public static readonly StatusPalette Status = new([Red.Base, Amber.Base, Green.Base]);

    // highlight tones
    public static readonly ColorPalette Gold = new ColorPalette("#a88b5e").Construct();
    public static readonly ColorPalette Maroon = new ColorPalette("#9b2236").Construct();

    // Intended to be used with `ModulateSelf` to darken / lighten something
    public static readonly ColorPalette AlphaModulate = new ColorPalette("#ffffff").Construct();

}
