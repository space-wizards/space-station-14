using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;

namespace Content.Client.Stylesheets.Stylesheets;

public partial class NanotrasenStylesheetFactory : IPaletteConfig
{
    public override ColorPalette PrimaryPalette => Palettes.Navy;
    public override ColorPalette SecondaryPalette => Palettes.Slate;
    public override ColorPalette PositivePalette => Palettes.Green;
    public override ColorPalette NegativePalette => Palettes.Red;
    public override ColorPalette HighlightPalette => Palettes.Gold;
}
