using Content.Client.Stylesheets.Palette;

namespace Content.Client.Stylesheets.Stylesheets;

public partial class SyndicateStylesheet
{
    public override ColorPalette PrimaryPalette => Palettes.Maroon;
    public override ColorPalette SecondaryPalette => Palettes.Dark;
    public override ColorPalette PositivePalette => Palettes.Green;
    public override ColorPalette NegativePalette => Palettes.Red;
    public override ColorPalette HighlightPalette => Palettes.Maroon;
}
