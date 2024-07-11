using Content.Client.Stylesheets.Redux.SheetletConfigs;

namespace Content.Client.Stylesheets.Redux.Stylesheets;

public partial class InterfaceStylesheet
{
    public override ColorPalette PrimaryPalette => Palettes.InterfacePrimary;
    public override ColorPalette SecondaryPalette => Palettes.InterfaceSecondary;
    public override ColorPalette PositivePalette => Palettes.PositiveGreen;
    public override ColorPalette NegativePalette => Palettes.NegativeRed;
    public override ColorPalette HighlightPalette => Palettes.HighlightYellow;
}
