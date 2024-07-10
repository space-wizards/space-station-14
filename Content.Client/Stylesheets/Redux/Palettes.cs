namespace Content.Client.Stylesheets.Redux;

/// <summary>
///     Stores all style palettes in one accessible location
/// </summary>
/// <remarks>
///     Technically not limited to only colors, can store like, standard padding amounts, and font sizes, maybe?
/// </remarks>
public static class Palettes
{
    public static readonly ColorPalette NanoPrimary = new(Color.FromHex("#575b7f"));
    public static readonly ColorPalette NanoSecondary = new(Color.FromHex("#5b5d6e"));

    public static readonly ColorPalette InterfacePrimary = new(Color.FromHex("#4a6173"));
    public static readonly ColorPalette InterfaceSecondary = new(Color.FromHex("#5e5e5e"));

    public static readonly ColorPalette PositiveGreen = new(Color.FromHex("#3e6c45"));
    public static readonly ColorPalette NegativeRed = new(Color.FromHex("#cf2f2f"));
    public static readonly ColorPalette HighlightYellow = new(Color.FromHex("#a88b5e"));

    // Intended to be used with `ModulateSelf` to darken / lighten something
    public static readonly ColorPalette AlphaModulate = new(Color.FromHex("#ffffff"));
}
