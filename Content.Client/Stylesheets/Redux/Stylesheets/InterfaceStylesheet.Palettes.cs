using Content.Client.Stylesheets.Redux.Colorspace;
using Content.Client.Stylesheets.Redux.NTSheetlets;
using Content.Client.Stylesheets.Redux.SheetletConfig;

namespace Content.Client.Stylesheets.Redux.Stylesheets;

public partial class InterfaceStylesheet : IPanelPalette
{
    public override ColorPalette PrimaryPalette => Palettes.InterfacePrimary;
    public override ColorPalette SecondaryPalette => Palettes.InterfaceSecondary;
    public override ColorPalette PositivePalette => Palettes.PositiveGreen;
    public override ColorPalette NegativePalette => Palettes.NegativeRed;
    public override ColorPalette HighlightPalette => Palettes.HighlightYellow;

    Color IPanelPalette.PanelLightColor => SecondaryPalette.BackgroundLight;
    Color IPanelPalette.PanelColor => SecondaryPalette.Background;
    Color IPanelPalette.PanelDarkColor => SecondaryPalette.BackgroundDark;
}
