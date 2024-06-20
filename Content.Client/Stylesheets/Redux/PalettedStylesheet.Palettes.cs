namespace Content.Client.Stylesheets.Redux;

public abstract partial class PalettedStylesheet
{
    public abstract Color[] PrimaryPalette { get; }

    public abstract Color[] SecondaryPalette { get; }

    public abstract Color[] PositivePalette { get; }

    public abstract Color[] NegativePalette { get; }

    public abstract Color[] HighlightPalette { get; }
}
