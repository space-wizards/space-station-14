using Content.Client.Stylesheets.Palette;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IPaletteConfig : ISheetletConfig
{
    ColorPalette PrimaryPalette { get; }
    ColorPalette SecondaryPalette { get; }
    ColorPalette PositivePalette { get; }
    ColorPalette NegativePalette { get; }
    ColorPalette HighlightPalette { get; }
}
