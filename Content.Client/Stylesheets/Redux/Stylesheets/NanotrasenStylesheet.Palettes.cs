using Content.Client.Stylesheets.Redux.Colorspace;
using Content.Client.Stylesheets.Redux.NTSheetlets;
using Content.Client.Stylesheets.Redux.SheetletConfig;
using Content.Client.Stylesheets.Redux.Sheetlets;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.Stylesheets;

public sealed partial class NanotrasenStylesheet : IPanelPalette, IStatusPalette
{
    public override ColorPalette PrimaryPalette => Palettes.NanoPrimary;
    public override ColorPalette SecondaryPalette => Palettes.NanoSecondary;
    public override ColorPalette PositivePalette => Palettes.PositiveGreen;
    public override ColorPalette NegativePalette => Palettes.NegativeRed;
    public override ColorPalette HighlightPalette => Palettes.HighlightYellow;

    Color IPanelPalette.PanelLightColor => SecondaryPalette.BackgroundLight;
    Color IPanelPalette.PanelColor => SecondaryPalette.Background;
    Color IPanelPalette.PanelDarkColor => SecondaryPalette.BackgroundDark;

    Color[] IStatusPalette.StatusColors => [NegativePalette.Base, HighlightPalette.Base, PositivePalette.Base];
}
