namespace Content.Client.Stylesheets.Redux.Stylesheets;

public sealed partial class NanotrasenStylesheet
{
    public override ColorPalette PrimaryPalette => Palettes.NanoPrimary;
    public override ColorPalette SecondaryPalette => Palettes.NanoSecondary;
    public override ColorPalette PositivePalette => Palettes.PositiveGreen;
    public override ColorPalette NegativePalette => Palettes.NegativeRed;
    public override ColorPalette HighlightPalette => Palettes.HighlightYellow;
}
