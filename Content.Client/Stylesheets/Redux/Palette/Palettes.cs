namespace Content.Client.Stylesheets.Redux.Palette;

/// <summary>
///     Stores all style palettes in one accessible location
/// </summary>
/// <remarks>
///     Technically not limited to only colors, can store like, standard padding amounts, and font sizes, maybe?
/// </remarks>
public static class Palettes
{
    public static readonly ColorPalette Navy = new ColorPalette{ Base = Color.FromHex("#4f5376"), ChromaNegativeShift = -0.012f }.Construct();
    public static readonly ColorPalette Cyan = new ColorPalette{ Base = Color.FromHex("#42586a"), ChromaNegativeShift = -0.012f }.Construct();
    public static readonly ColorPalette Slate = new ColorPalette{ Base = Color.FromHex("#545562")}.Construct();
    public static readonly ColorPalette Neutral = new ColorPalette{ Base = Color.FromHex("#555555")}.Construct();

    public static readonly ColorPalette Red = new ColorPalette{ Base = Color.FromHex("#cf2f2f")}.Construct();
    public static readonly ColorPalette Amber = new ColorPalette{ Base = Color.FromHex("#c18e36")}.Construct();
    public static readonly ColorPalette Green = new ColorPalette{ Base = Color.FromHex("#3c854a")}.Construct();
    public static readonly StatusPalette Status = new([Red.Base, Amber.Base, Green.Base]);

    public static readonly ColorPalette Gold = new ColorPalette{ Base = Color.FromHex("#a88b5e")}.Construct();
    public static readonly ColorPalette Maroon = new ColorPalette{ Base = Color.FromHex("#9b2236")}.Construct();

    // Intended to be used with `ModulateSelf` to darken / lighten something
    public static readonly ColorPalette AlphaModulate = new ColorPalette{ Base = Color.FromHex("#ffffff")}.Construct();

}
