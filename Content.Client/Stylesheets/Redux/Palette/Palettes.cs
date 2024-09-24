namespace Content.Client.Stylesheets.Redux.Palette;

/// <summary>
///     Stores all style palettes in one accessible location
/// </summary>
/// <remarks>
///     Technically not limited to only colors, can store like, standard padding amounts, and font sizes, maybe?
/// </remarks>
public static class Palettes
{
    public static readonly ColorPalette Navy = new(Color.FromHex("#575b7f"));
    public static readonly ColorPalette Cyan = new(Color.FromHex("#4a6173"));
    public static readonly ColorPalette Slate = new(Color.FromHex("#5b5d6e"));
    public static readonly ColorPalette Neutral = new(Color.FromHex("#5e5e5e"));

    public static readonly ColorPalette Red = new(Color.FromHex("#cf2f2f"));
    public static readonly ColorPalette Amber = new(Color.FromHex("#c18e36"));
    public static readonly ColorPalette Green = new(Color.FromHex("#3c854a"));
    public static readonly StatusPalette Status = new([Red.Base, Amber.Base, Green.Base]);

    public static readonly ColorPalette Gold = new(Color.FromHex("#a88b5e"));
    public static readonly ColorPalette Maroon = new(Color.FromHex("#9b2236"));

    // Intended to be used with `ModulateSelf` to darken / lighten something
    public static readonly ColorPalette AlphaModulate = new(Color.FromHex("#ffffff"));

}
