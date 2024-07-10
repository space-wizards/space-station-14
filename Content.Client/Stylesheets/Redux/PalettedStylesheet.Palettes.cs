using Content.Client.Stylesheets.Redux.Colorspace;
using Content.Client.Stylesheets.Redux.SheetletConfig;

namespace Content.Client.Stylesheets.Redux;

public abstract partial class PalettedStylesheet
{
    public abstract ColorPalette PrimaryPalette { get; }
    public abstract ColorPalette SecondaryPalette { get; }
    public abstract ColorPalette PositivePalette { get; }
    public abstract ColorPalette NegativePalette { get; }
    public abstract ColorPalette HighlightPalette { get; }
}
